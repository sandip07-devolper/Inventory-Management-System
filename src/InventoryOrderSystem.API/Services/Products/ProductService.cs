using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.Products;
using InventoryOrderSystem.API.Mapping;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Services.Products;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;

    public ProductService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query)
    {
        var products = _dbContext.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        if (query.CategoryId.HasValue)
            products = products.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.IsActive.HasValue)
            products = products.Where(p => p.IsActive == query.IsActive.Value);

        var totalCount = await products.CountAsync();

        var items = await products
            .OrderBy(p => p.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => p.ToDto())
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        await EnsureCategoryExistsAsync(request.CategoryId);
        await EnsureSkuIsUniqueAsync(request.Sku);

        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            CostPrice = request.CostPrice,
            ReorderLevel = request.ReorderLevel,
            CategoryId = request.CategoryId,
            QuantityOnHand = 0
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return (await _dbContext.Products.Include(p => p.Category).FirstAsync(p => p.Id == product.Id)).ToDto();
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await _dbContext.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

        await EnsureCategoryExistsAsync(request.CategoryId);
        await EnsureSkuIsUniqueAsync(request.Sku, excludingId: id);

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Description = request.Description;
        product.UnitPrice = request.UnitPrice;
        product.CostPrice = request.CostPrice;
        product.ReorderLevel = request.ReorderLevel;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        // Category may have changed - reload the navigation for an accurate DTO.
        await _dbContext.Entry(product).Reference(p => p.Category).LoadAsync();

        return product.ToDto();
    }

    /// <summary>
    /// Soft-deletes (deactivates) a product rather than removing it, preserving
    /// historical references from past orders/stock transactions.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

        product.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var exists = await _dbContext.Categories.AnyAsync(c => c.Id == categoryId);
        if (!exists)
            throw new NotFoundException(nameof(Category), categoryId);
    }

    private async Task EnsureSkuIsUniqueAsync(string sku, int? excludingId = null)
    {
        var skuTaken = await _dbContext.Products
            .AnyAsync(p => p.Sku == sku && (excludingId == null || p.Id != excludingId));

        if (skuTaken)
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
    }
}
