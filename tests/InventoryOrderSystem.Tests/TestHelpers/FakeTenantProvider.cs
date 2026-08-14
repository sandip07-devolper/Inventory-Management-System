using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Tests.TestHelpers;

public class FakeTenantProvider : ITenantProvider
{
    private readonly int? _tenantId;

    public FakeTenantProvider(int? tenantId = 1)
    {
        _tenantId = tenantId;
    }

    public int? GetTenantId() => _tenantId;
}
