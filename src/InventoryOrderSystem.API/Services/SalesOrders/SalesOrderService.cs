using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.SalesOrders;
using InventoryOrderSystem.API.Mapping;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Enums;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryOrderSystem.API.Services.SalesOrders;

public class SalesOrderService : ISalesOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(AppDbContext dbContext, ILogger<SalesOrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<SalesOrderDto>> GetAllAsync(SalesOrderQuery query)
    {
        IQueryable<SalesOrder> orders = _dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsSplitQuery(); // avoids row duplication from the Items join while paging

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<SalesOrderStatus>(query.Status, ignoreCase: true, out var status))
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

        return new PagedResult<SalesOrderDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SalesOrderDto> GetByIdAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        return order.ToDto();
    }

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request)
    {
        var customerExists = await _dbContext.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists)
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        await EnsureProductsExistAsync(request.Items.Select(i => i.ProductId));

        var order = new SalesOrder
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            CustomerId = request.CustomerId,
            Status = SalesOrderStatus.Draft,
            OrderDate = DateTime.UtcNow,
            Notes = request.Notes,
            Items = request.Items.Select(i => new SalesOrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        _dbContext.SalesOrders.Add(order);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} created for customer {CustomerId} with {ItemCount} line items, total {TotalAmount:C}",
            order.OrderNumber, order.CustomerId, order.Items.Count, order.TotalAmount);

        return (await LoadOrderAsync(order.Id)).ToDto();
    }

    public async Task<SalesOrderDto> UpdateAsync(int id, UpdateSalesOrderRequest request)
    {
        var order = await LoadOrderAsync(id);
        EnsureIsDraft(order, "edited");

        var customerExists = await _dbContext.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists)
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        await EnsureProductsExistAsync(request.Items.Select(i => i.ProductId));

        _dbContext.SalesOrderItems.RemoveRange(order.Items);

        order.CustomerId = request.CustomerId;
        order.Notes = request.Notes;
        order.Items = request.Items.Select(i => new SalesOrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        await _dbContext.SaveChangesAsync();

        return (await LoadOrderAsync(order.Id)).ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException(nameof(SalesOrder), id);

        EnsureIsDraft(order, "deleted");

        _dbContext.SalesOrders.Remove(order); // cascades to items
        await _dbContext.SaveChangesAsync();
    }

    public async Task<SalesOrderDto> FulfillAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        EnsureIsDraft(order, "fulfilled");

        var shortages = order.Items
            .Where(i => i.Product.QuantityOnHand < i.Quantity)
            .Select(i => $"{i.Product.Name} (SKU {i.Product.Sku}): requested {i.Quantity}, only {i.Product.QuantityOnHand} in stock")
            .ToList();

        if (shortages.Count > 0)
        {
            _logger.LogWarning(
                "Sales order {OrderNumber} fulfillment REJECTED due to insufficient stock: {Shortages}",
                order.OrderNumber, string.Join("; ", shortages));

            throw new ConflictException(
                $"Cannot fulfill order '{order.OrderNumber}' due to insufficient stock - {string.Join("; ", shortages)}");
        }

        // All stock deductions and the status change commit together in a single
        // SaveChanges call, so a partial fulfillment can never be persisted.
        foreach (var item in order.Items)
        {
            item.Product.QuantityOnHand -= item.Quantity;
        }

        order.Status = SalesOrderStatus.Fulfilled;
        order.FulfilledDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} fulfilled: stock deducted for {ItemCount} products - {Items}",
            order.OrderNumber,
            order.Items.Count,
            string.Join(", ", order.Items.Select(i => $"{i.Product.Sku}:-{i.Quantity}")));

        return order.ToDto();
    }

    public async Task<SalesOrderDto> CancelAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        EnsureIsDraft(order, "cancelled");

        order.Status = SalesOrderStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Sales order {OrderNumber} cancelled", order.OrderNumber);

        return order.ToDto();
    }

    private async Task<SalesOrder> LoadOrderAsync(int id)
    {
        return await _dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException(nameof(SalesOrder), id);
    }

    private static void EnsureIsDraft(SalesOrder order, string action)
    {
        if (order.Status != SalesOrderStatus.Draft)
            throw new ConflictException(
                $"Sales order '{order.OrderNumber}' cannot be {action} because it is already {order.Status}.");
    }

    private async Task EnsureProductsExistAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        var validCount = await _dbContext.Products.CountAsync(p => ids.Contains(p.Id));
        if (validCount != ids.Count)
            throw new NotFoundException("One or more products in the sales order could not be found.");
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var next = await _dbContext.SalesOrders.CountAsync() + 1;
        var orderNumber = $"SO-{next:D6}";

        while (await _dbContext.SalesOrders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            next++;
            orderNumber = $"SO-{next:D6}";
        }

        return orderNumber;
    }
}
