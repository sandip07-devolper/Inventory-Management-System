using InventoryOrderSystem.API.DTOs.Suppliers;
using InventoryOrderSystem.Domain.Entities;

namespace InventoryOrderSystem.API.Mapping;

public static class SupplierMappingExtensions
{
    public static SupplierDto ToDto(this Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        ContactPerson = supplier.ContactPerson,
        Email = supplier.Email,
        Phone = supplier.Phone,
        Address = supplier.Address,
        IsActive = supplier.IsActive
    };
}
