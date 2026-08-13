namespace InventoryOrderSystem.Infrastructure.Data;

/// <summary>
/// Resolves the current request's TenantId (from the JWT claims).
/// Returning null means "no tenant context" (e.g. system/setup operations).
/// </summary>
public interface ITenantProvider
{
    int? GetTenantId();
}
