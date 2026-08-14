using InventoryOrderSystem.API.Services.Reports;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Tests.TestHelpers;
using Xunit;

namespace InventoryOrderSystem.Tests.Services;

public class ReportServiceTests
{
    private static async Task<AppDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();

        var category = new Category { Name = "Electronics" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product
            {
                Sku = "LOW-1", Name = "Low Stock Widget", CategoryId = category.Id,
                QuantityOnHand = 2, ReorderLevel = 10, CostPrice = 5m, UnitPrice = 9m
            },
            new Product
            {
                Sku = "OK-1", Name = "Well Stocked Widget", CategoryId = category.Id,
                QuantityOnHand = 50, ReorderLevel = 10, CostPrice = 3m, UnitPrice = 6m
            },
            new Product
            {
                Sku = "ZERO-1", Name = "Zero Stock Widget", CategoryId = category.Id,
                QuantityOnHand = 0, ReorderLevel = 5, CostPrice = 2m, UnitPrice = 4m
            });

        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetLowStockReportAsync_ReturnsOnlyProductsAtOrBelowReorderLevel()
    {
        var db = await SeedAsync();
        var service = new ReportService(db);

        var report = await service.GetLowStockReportAsync();

        Assert.Equal(2, report.TotalItemsBelowReorder); // LOW-1 and ZERO-1
        Assert.Contains(report.Items, i => i.Sku == "LOW-1");
        Assert.Contains(report.Items, i => i.Sku == "ZERO-1");
        Assert.DoesNotContain(report.Items, i => i.Sku == "OK-1");
    }

    [Fact]
    public async Task GetLowStockReportAsync_CalculatesShortageQuantityCorrectly()
    {
        var db = await SeedAsync();
        var service = new ReportService(db);

        var report = await service.GetLowStockReportAsync();

        var lowStockItem = report.Items.Single(i => i.Sku == "LOW-1");
        Assert.Equal(8, lowStockItem.ShortageQuantity); // 10 reorder - 2 on hand
    }

    [Fact]
    public async Task GetInventoryValuationReportAsync_ExcludesProductsWithZeroStock()
    {
        var db = await SeedAsync();
        var service = new ReportService(db);

        var report = await service.GetInventoryValuationReportAsync();

        Assert.DoesNotContain(report.Items, i => i.Sku == "ZERO-1");
        Assert.Equal(2, report.Items.Count);
    }

    [Fact]
    public async Task GetInventoryValuationReportAsync_CalculatesTotalsCorrectly()
    {
        var db = await SeedAsync();
        var service = new ReportService(db);

        var report = await service.GetInventoryValuationReportAsync();

        // LOW-1: 2 * 5 = 10 cost, 2 * 9 = 18 retail
        // OK-1: 50 * 3 = 150 cost, 50 * 6 = 300 retail
        Assert.Equal(160m, report.TotalCostValue);
        Assert.Equal(318m, report.TotalRetailValue);
        Assert.Equal(52, report.TotalUnitsOnHand);
    }
}
