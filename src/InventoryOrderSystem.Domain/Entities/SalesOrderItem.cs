using InventoryOrderSystem.Domain.Common;

namespace InventoryOrderSystem.Domain.Entities;

public class SalesOrderItem : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
