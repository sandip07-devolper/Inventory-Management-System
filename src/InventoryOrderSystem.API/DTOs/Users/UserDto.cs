namespace InventoryOrderSystem.API.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
