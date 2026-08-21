using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.Users;

public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("^(Admin|Staff)$", ErrorMessage = "Role must be 'Admin' or 'Staff'.")]
    public string Role { get; set; } = "Staff";
}
