namespace InventoryOrderSystem.API.DTOs.Reports;

public class InventoryValuationItemDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public decimal CostPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
}

public class InventoryValuationReportDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int TotalUnitsOnHand { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public List<InventoryValuationItemDto> Items { get; set; } = new();
}
