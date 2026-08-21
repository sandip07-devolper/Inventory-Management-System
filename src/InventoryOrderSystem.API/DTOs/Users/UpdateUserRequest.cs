using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.Users;

public class UpdateUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression("^(Admin|Staff)$", ErrorMessage = "Role must be 'Admin' or 'Staff'.")]
    public string Role { get; set; } = "Staff";

    public bool IsActive { get; set; } = true;
}
