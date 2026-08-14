using InventoryOrderSystem.Domain.Common;

namespace InventoryOrderSystem.Domain.Entities;

public class Category : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
