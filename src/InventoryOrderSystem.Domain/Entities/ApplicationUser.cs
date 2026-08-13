using Microsoft.AspNetCore.Identity;

namespace InventoryOrderSystem.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
