using InventoryOrderSystem.API.DTOs.Categories;
using InventoryOrderSystem.API.Mapping;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Services.Categories;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        return await _dbContext.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _dbContext.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Category), id);

        return category.ToDto();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var nameExists = await _dbContext.Categories.AnyAsync(c => c.Name == request.Name);
        if (nameExists)
            throw new ConflictException($"A category named '{request.Name}' already exists.");

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Category), id);

        var nameTaken = await _dbContext.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id);
        if (nameTaken)
            throw new ConflictException($"A category named '{request.Name}' already exists.");

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return category.ToDto();
    }

    /// <summary>
    /// Soft-deletes (deactivates) a category. Categories are never hard-deleted
    /// because existing products may still reference them.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Category), id);

        category.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }
}
