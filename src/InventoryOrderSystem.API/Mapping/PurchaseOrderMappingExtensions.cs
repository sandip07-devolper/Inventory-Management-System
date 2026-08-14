using InventoryOrderSystem.API.DTOs.PurchaseOrders;
using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Mapping;

public static class PurchaseOrderMappingExtensions
{
    public static PurchaseOrderItemDto ToDto(this PurchaseOrderItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.Product?.Name ?? string.Empty,
        Sku = item.Product?.Sku ?? string.Empty,
        Quantity = item.Quantity,
        UnitCost = item.UnitCost,
        LineTotal = item.Quantity * item.UnitCost
    };

    public static PurchaseOrderDto ToDto(this PurchaseOrder order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        SupplierId = order.SupplierId,
        SupplierName = order.Supplier?.Name ?? string.Empty,
        Status = order.Status.ToString(),
        OrderDate = order.OrderDate,
        ReceivedDate = order.ReceivedDate,
        Notes = order.Notes,
        TotalAmount = order.TotalAmount,
        Items = order.Items?.Select(i => i.ToDto()).ToList() ?? new List<PurchaseOrderItemDto>()
    };
}
