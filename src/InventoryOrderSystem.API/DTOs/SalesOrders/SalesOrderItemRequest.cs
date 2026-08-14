using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.SalesOrders;

public class SalesOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
