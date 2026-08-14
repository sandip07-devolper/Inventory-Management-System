using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.PurchaseOrders;

public class CreatePurchaseOrderRequest
{
    [Required]
    public int SupplierId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required, MinLength(1, ErrorMessage = "A purchase order must have at least one item.")]
    public List<PurchaseOrderItemRequest> Items { get; set; } = new();
}
