using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.PurchaseOrders;

public class PurchaseOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}
