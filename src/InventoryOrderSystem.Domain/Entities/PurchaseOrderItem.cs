using InventoryOrderSystem.Domain.Common;

namespace InventoryOrderSystem.Domain.Entities;

public class PurchaseOrderItem : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
