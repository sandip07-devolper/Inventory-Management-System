using System.ComponentModel.DataAnnotations;

namespace InventoryOrderSystem.API.DTOs.Customers;

public class CreateCustomerRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }
}
