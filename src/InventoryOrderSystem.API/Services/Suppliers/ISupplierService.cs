using InventoryOrderSystem.API.DTOs.Suppliers;

namespace InventoryOrderSystem.API.Services.Suppliers;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllAsync();
    Task<SupplierDto> GetByIdAsync(int id);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request);
    Task DeleteAsync(int id);
}
