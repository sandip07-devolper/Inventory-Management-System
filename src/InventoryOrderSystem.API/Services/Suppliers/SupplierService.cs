using InventoryOrderSystem.API.DTOs.Suppliers;
using InventoryOrderSystem.API.Mapping;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Services.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _dbContext;

    public SupplierService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        return await _dbContext.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => s.ToDto())
            .ToListAsync();
    }

    public async Task<SupplierDto> GetByIdAsync(int id)
    {
        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException(nameof(Supplier), id);

        return supplier.ToDto();
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request)
    {
        var nameExists = await _dbContext.Suppliers.AnyAsync(s => s.Name == request.Name);
        if (nameExists)
            throw new ConflictException($"A supplier named '{request.Name}' already exists.");

        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        return supplier.ToDto();
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request)
    {
        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException(nameof(Supplier), id);

        var nameTaken = await _dbContext.Suppliers.AnyAsync(s => s.Name == request.Name && s.Id != id);
        if (nameTaken)
            throw new ConflictException($"A supplier named '{request.Name}' already exists.");

        supplier.Name = request.Name;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return supplier.ToDto();
    }

    /// <summary>
    /// Soft-deletes (deactivates) a supplier - past purchase orders keep referencing it.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException(nameof(Supplier), id);

        supplier.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }
}
