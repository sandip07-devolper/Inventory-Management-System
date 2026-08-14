using InventoryOrderSystem.API.DTOs.Common;

namespace InventoryOrderSystem.API.DTOs.PurchaseOrders;

public class PurchaseOrderQuery : PaginationQuery
{
    /// <summary>Optional status filter: Draft, Received, or Cancelled.</summary>
    public string? Status { get; set; }
}
