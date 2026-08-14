namespace InventoryOrderSystem.API.DTOs.SalesOrders;

public class SalesOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? FulfilledDate { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}
