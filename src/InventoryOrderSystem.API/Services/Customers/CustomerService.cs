using InventoryOrderSystem.API.DTOs.Customers;
using InventoryOrderSystem.API.Mapping;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Services.Customers;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _dbContext;

    public CustomerService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        return await _dbContext.Customers
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync();
    }

    public async Task<CustomerDto> GetByIdAsync(int id)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Customer), id);

        return customer.ToDto();
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        var nameExists = await _dbContext.Customers.AnyAsync(c => c.Name == request.Name);
        if (nameExists)
            throw new ConflictException($"A customer named '{request.Name}' already exists.");

        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        return customer.ToDto();
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Customer), id);

        var nameTaken = await _dbContext.Customers.AnyAsync(c => c.Name == request.Name && c.Id != id);
        if (nameTaken)
            throw new ConflictException($"A customer named '{request.Name}' already exists.");

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = request.Address;
        customer.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return customer.ToDto();
    }

    /// <summary>
    /// Soft-deletes (deactivates) a customer - past sales orders keep referencing it.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Customer), id);

        customer.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }
}
