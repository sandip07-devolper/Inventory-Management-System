namespace InventoryOrderSystem.Domain.Common;

/// <summary>
/// Marks an entity as tenant-scoped. AppDbContext automatically applies
/// a global query filter and stamps TenantId on insert for any entity
/// implementing this interface.
/// </summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}
