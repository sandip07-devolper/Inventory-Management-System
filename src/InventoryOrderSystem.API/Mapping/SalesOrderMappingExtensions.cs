using InventoryOrderSystem.API.DTOs.SalesOrders;
using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Mapping;

public static class SalesOrderMappingExtensions
{
    public static SalesOrderItemDto ToDto(this SalesOrderItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.Product?.Name ?? string.Empty,
        Sku = item.Product?.Sku ?? string.Empty,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        LineTotal = item.Quantity * item.UnitPrice
    };

    public static SalesOrderDto ToDto(this SalesOrder order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer?.Name ?? string.Empty,
        Status = order.Status.ToString(),
        OrderDate = order.OrderDate,
        FulfilledDate = order.FulfilledDate,
        Notes = order.Notes,
        TotalAmount = order.TotalAmount,
        Items = order.Items?.Select(i => i.ToDto()).ToList() ?? new List<SalesOrderItemDto>()
    };
}
