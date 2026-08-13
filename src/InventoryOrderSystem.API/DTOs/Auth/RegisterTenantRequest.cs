using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.Auth;

public class RegisterTenantRequest
{
    [Required, MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
