using InventoryOrderSystem.Domain.Common;
using InventoryOrderSystem.Domain.Enums;

namespace InventoryOrderSystem.Domain.Entities;

public class PurchaseOrder : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
