using InventoryOrderSystem.Domain.Common;

namespace InventoryOrderSystem.Domain.Entities;

public class Product : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int ReorderLevel { get; set; }

    /// <summary>
    /// Current stock on hand. Not directly editable via the Products API -
    /// it changes only through stock/purchase/sales transactions.
    /// </summary>
    public int QuantityOnHand { get; set; }

    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
