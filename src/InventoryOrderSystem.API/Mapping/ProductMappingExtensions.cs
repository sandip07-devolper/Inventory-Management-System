using InventoryOrderSystem.API.DTOs.Products;
using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Mapping;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        Description = product.Description,
        UnitPrice = product.UnitPrice,
        CostPrice = product.CostPrice,
        ReorderLevel = product.ReorderLevel,
        QuantityOnHand = product.QuantityOnHand,
        IsActive = product.IsActive,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty
    };
}
