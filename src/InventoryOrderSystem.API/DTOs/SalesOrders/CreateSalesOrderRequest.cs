using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.SalesOrders;

public class CreateSalesOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required, MinLength(1, ErrorMessage = "A sales order must have at least one item.")]
    public List<SalesOrderItemRequest> Items { get; set; } = new();
}
