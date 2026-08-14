namespace InventoryOrderSystem.API.DTOs.Reports;

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public int ShortageQuantity { get; set; }
}

public class LowStockReportDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int TotalItemsBelowReorder { get; set; }
    public List<LowStockProductDto> Items { get; set; } = new();
}
