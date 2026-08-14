using InventoryOrderSystem.API.DTOs.Reports;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Services.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _dbContext;

    public ReportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LowStockReportDto> GetLowStockReportAsync()
    {
        var items = await _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.QuantityOnHand <= p.ReorderLevel)
            .OrderBy(p => p.QuantityOnHand - p.ReorderLevel) // most-short first
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                Sku = p.Sku,
                ProductName = p.Name,
                CategoryName = p.Category.Name,
                QuantityOnHand = p.QuantityOnHand,
                ReorderLevel = p.ReorderLevel,
                ShortageQuantity = p.ReorderLevel - p.QuantityOnHand
            })
            .ToListAsync();

        return new LowStockReportDto
        {
            TotalItemsBelowReorder = items.Count,
            Items = items
        };
    }

    public async Task<InventoryValuationReportDto> GetInventoryValuationReportAsync()
    {
        // Deliberately includes inactive products: physical stock still sitting
        // in the warehouse has real value even if the product has been retired
        // from sale.
        var items = await _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.QuantityOnHand > 0)
            .OrderByDescending(p => p.QuantityOnHand * p.CostPrice)
            .Select(p => new InventoryValuationItemDto
            {
                ProductId = p.Id,
                Sku = p.Sku,
                ProductName = p.Name,
                CategoryName = p.Category.Name,
                QuantityOnHand = p.QuantityOnHand,
                CostPrice = p.CostPrice,
                UnitPrice = p.UnitPrice,
                TotalCostValue = p.QuantityOnHand * p.CostPrice,
                TotalRetailValue = p.QuantityOnHand * p.UnitPrice
            })
            .ToListAsync();

        return new InventoryValuationReportDto
        {
            TotalUnitsOnHand = items.Sum(i => i.QuantityOnHand),
            TotalCostValue = items.Sum(i => i.TotalCostValue),
            TotalRetailValue = items.Sum(i => i.TotalRetailValue),
            Items = items
        };
    }
}
