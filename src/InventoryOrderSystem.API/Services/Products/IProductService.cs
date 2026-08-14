using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.Products;

namespace InventoryOrderSystem.API.Services.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query);
    Task<ProductDto> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request);
    Task DeleteAsync(int id);
}
