using InventoryOrderSystem.API.DTOs.Common;

namespace InventoryOrderSystem.API.DTOs.Products;

public class ProductQuery : PaginationQuery
{
    /// <summary>Matches against Name or Sku (case-insensitive, partial match).</summary>
    public string? Search { get; set; }

    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}
