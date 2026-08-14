using InventoryOrderSystem.API.DTOs.Categories;
using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Mapping;

public static class CategoryMappingExtensions
{
    public static CategoryDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        ProductCount = category.Products?.Count ?? 0
    };
}
