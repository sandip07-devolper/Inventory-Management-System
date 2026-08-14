using InventoryOrderSystem.Domain.Common;
using InventoryOrderSystem.Domain.Enums;

namespace InventoryOrderSystem.Domain.Entities;

public class SalesOrder : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledDate { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}
