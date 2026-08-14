using InventoryOrderSystem.API.DTOs.Customers;
using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Mapping;

public static class CustomerMappingExtensions
{
    public static CustomerDto ToDto(this Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        Phone = customer.Phone,
        Address = customer.Address,
        IsActive = customer.IsActive
    };
}
