using InventoryOrderSystem.API.DTOs.Products;
using InventoryOrderSystem.API.Services.Products;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Tests.TestHelpers;
using Xunit;

namespace InventoryOrderSystem.Tests.Services;

public class ProductServiceTests
{
    private static async Task<InventoryOrderSystem.Infrastructure.Data.AppDbContext> SeedAsync(int productCount)
    {
        var db = TestDbContextFactory.Create();

        var category = new Category { Name = "Electronics" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        for (var i = 1; i <= productCount; i++)
        {
            db.Products.Add(new Product
            {
                Sku = $"SKU-{i:D3}",
                Name = $"Product {i:D3}",
                CategoryId = category.Id,
                UnitPrice = 10m,
                CostPrice = 5m
            });
        }

        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetAllAsync_RespectsPageSize()
    {
        var db = await SeedAsync(25);
        var service = new ProductService(db);

        var result = await service.GetAllAsync(new ProductQuery { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_SecondPage_ReturnsRemainingItems()
    {
        var db = await SeedAsync(25);
        var service = new ProductService(db);

        var result = await service.GetAllAsync(new ProductQuery { PageNumber = 3, PageSize = 10 });

        Assert.Equal(5, result.Items.Count); // 25 total, page 3 of size 10 -> 5 left
    }

    [Fact]
    public async Task GetAllAsync_FiltersBySearchTerm()
    {
        var db = await SeedAsync(5);
        var service = new ProductService(db);

        var result = await service.GetAllAsync(new ProductQuery { Search = "Product 003" });

        Assert.Single(result.Items);
        Assert.Equal("SKU-003", result.Items[0].Sku);
    }

    [Fact]
    public async Task PageSize_AboveMax_IsClampedTo100()
    {
        var query = new ProductQuery { PageSize = 500 };

        Assert.Equal(100, query.PageSize);
    }
}
