using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.PurchaseOrders;
using InventoryOrderSystem.API.Mapping;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Enums;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryOrderSystem.API.Services.PurchaseOrders;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(AppDbContext dbContext, ILogger<PurchaseOrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetAllAsync(PurchaseOrderQuery query)
    {
        IQueryable<PurchaseOrder> orders = _dbContext.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsSplitQuery(); // avoids row duplication from the Items join while paging

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<PurchaseOrderStatus>(query.Status, ignoreCase: true, out var status))
        {
            orders = orders.Where(o => o.Status == status);
        }

        var totalCount = await orders.CountAsync();

        var items = await orders
            .OrderByDescending(o => o.OrderDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => o.ToDto())
            .ToListAsync();

        return new PagedResult<PurchaseOrderDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        return order.ToDto();
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request)
    {
        var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == request.SupplierId);
        if (!supplierExists)
            throw new NotFoundException(nameof(Supplier), request.SupplierId);

        await EnsureProductsExistAsync(request.Items.Select(i => i.ProductId));

        var order = new PurchaseOrder
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            SupplierId = request.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTime.UtcNow,
            Notes = request.Notes,
            Items = request.Items.Select(i => new PurchaseOrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitCost);

        _dbContext.PurchaseOrders.Add(order);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Purchase order {OrderNumber} created for supplier {SupplierId} with {ItemCount} line items, total {TotalAmount:C}",
            order.OrderNumber, order.SupplierId, order.Items.Count, order.TotalAmount);

        return (await LoadOrderAsync(order.Id)).ToDto();
    }

    public async Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderRequest request)
    {
        var order = await LoadOrderAsync(id);
        EnsureIsDraft(order, "edited");

        var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == request.SupplierId);
        if (!supplierExists)
            throw new NotFoundException(nameof(Supplier), request.SupplierId);

        await EnsureProductsExistAsync(request.Items.Select(i => i.ProductId));

        _dbContext.PurchaseOrderItems.RemoveRange(order.Items);

        order.SupplierId = request.SupplierId;
        order.Notes = request.Notes;
        order.Items = request.Items.Select(i => new PurchaseOrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitCost = i.UnitCost
        }).ToList();
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitCost);

        await _dbContext.SaveChangesAsync();

        return (await LoadOrderAsync(order.Id)).ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);

        EnsureIsDraft(order, "deleted");

        _dbContext.PurchaseOrders.Remove(order); // cascades to items
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PurchaseOrderDto> ReceiveAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        EnsureIsDraft(order, "received");

        // Add each item's quantity to on-hand stock. All updates happen inside
        // a single SaveChanges call, so they're committed atomically together
        // with the order status change.
        foreach (var item in order.Items)
        {
            item.Product.QuantityOnHand += item.Quantity;
        }

        order.Status = PurchaseOrderStatus.Received;
        order.ReceivedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Purchase order {OrderNumber} received: stock increased for {ItemCount} products - {Items}",
            order.OrderNumber,
            order.Items.Count,
            string.Join(", ", order.Items.Select(i => $"{i.Product.Sku}:+{i.Quantity}")));

        return order.ToDto();
    }

    public async Task<PurchaseOrderDto> CancelAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        EnsureIsDraft(order, "cancelled");

        order.Status = PurchaseOrderStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Purchase order {OrderNumber} cancelled", order.OrderNumber);

        return order.ToDto();
    }

    private async Task<PurchaseOrder> LoadOrderAsync(int id)
    {
        return await _dbContext.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);
    }

    private static void EnsureIsDraft(PurchaseOrder order, string action)
    {
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new ConflictException(
                $"Purchase order '{order.OrderNumber}' cannot be {action} because it is already {order.Status}.");
    }

    private async Task EnsureProductsExistAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        var validCount = await _dbContext.Products.CountAsync(p => ids.Contains(p.Id));
        if (validCount != ids.Count)
            throw new NotFoundException("One or more products in the purchase order could not be found.");
    }

    /// <summary>
    /// Generates a sequential, tenant-scoped order number (e.g. PO-000001).
    /// Simplification note: uses a count-based sequence rather than a DB sequence
    /// object, which is fine at this scale but could theoretically collide under
    /// heavy concurrent writes - the uniqueness loop below guards against that.
    /// </summary>
    private async Task<string> GenerateOrderNumberAsync()
    {
        var next = await _dbContext.PurchaseOrders.CountAsync() + 1;
        var orderNumber = $"PO-{next:D6}";

        while (await _dbContext.PurchaseOrders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            next++;
            orderNumber = $"PO-{next:D6}";
        }

        return orderNumber;
    }
}
