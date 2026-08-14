using InventoryOrderSystem.API.DTOs.Common;

namespace InventoryOrderSystem.API.DTOs.SalesOrders;

public class SalesOrderQuery : PaginationQuery
{
    /// <summary>Optional status filter: Draft, Fulfilled, or Cancelled.</summary>
    public string? Status { get; set; }
}
