using InventoryOrderSystem.API.DTOs.Users;

namespace InventoryOrderSystem.API.Services.Users;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserRequest request);

    /// <summary>currentUserId is the caller's own id - used to block self-deactivation.</summary>
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, int currentUserId);

    /// <summary>currentUserId is the caller's own id - used to block self-deactivation.</summary>
    Task DeactivateAsync(int id, int currentUserId);
}
