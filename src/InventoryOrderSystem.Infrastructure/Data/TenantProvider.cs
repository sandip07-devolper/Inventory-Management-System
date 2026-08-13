using Microsoft.AspNetCore.Http;

namespace InventoryOrderSystem.Infrastructure.Data;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetTenantId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenantId");
        return claim is not null && int.TryParse(claim.Value, out var tenantId) ? tenantId : null;
    }
}
