using InventoryOrderSystem.API.DTOs.Customers;

namespace InventoryOrderSystem.API.Services.Customers;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto> GetByIdAsync(int id);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request);
    Task DeleteAsync(int id);
}
