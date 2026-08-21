using InventoryOrderSystem.API.DTOs.Users;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Services.Users;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantProvider _tenantProvider;

    public UserService(UserManager<ApplicationUser> userManager, ITenantProvider tenantProvider)
    {
        _userManager = userManager;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        // UserManager.Users is already tenant-scoped by AppDbContext's query filter
        // on ApplicationUser - no manual tenant filtering needed here.
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(await ToDtoAsync(user));
        }
        return result;
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException(nameof(ApplicationUser), id);

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var normalizedEmail = _userManager.NormalizeEmail(request.Email);

        // Same reasoning as AuthController.Register: email must be unique globally,
        // not just within this tenant, since login has to find a user by email
        // before it knows their tenant.
        var emailExists = await _userManager.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail);

        if (emailExists)
            throw new ConflictException("An account with this email already exists.");

        var tenantId = _tenantProvider.GetTenantId()
            ?? throw new InvalidOperationException("No tenant context available for the current request.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            TenantId = tenantId,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new ConflictException(string.Join(" ", createResult.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, int currentUserId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException(nameof(ApplicationUser), id);

        if (id == currentUserId && !request.IsActive)
            throw new ConflictException("You cannot deactivate your own account.");

        user.FullName = request.FullName;
        user.IsActive = request.IsActive;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, request.Role);
        }

        return await ToDtoAsync(user);
    }

    public async Task DeactivateAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new ConflictException("You cannot deactivate your own account.");

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException(nameof(ApplicationUser), id);

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = roles
        };
    }
}
