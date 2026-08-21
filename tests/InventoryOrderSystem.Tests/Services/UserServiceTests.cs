using InventoryOrderSystem.API.DTOs.Users;
using InventoryOrderSystem.API.Services.Users;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace InventoryOrderSystem.Tests.Services;

public class UserServiceTests
{
    private static async Task<(UserService Service, ApplicationUser Admin)> SeedAsync()
    {
        var (_, userManager, roleManager) = IdentityTestFactory.Create();

        await roleManager.CreateAsync(new IdentityRole<int>("Admin"));
        await roleManager.CreateAsync(new IdentityRole<int>("Staff"));

        var admin = new ApplicationUser
        {
            UserName = "admin@test.com",
            Email = "admin@test.com",
            FullName = "Admin User",
            TenantId = 1,
            IsActive = true
        };
        await userManager.CreateAsync(admin, "Password123");
        await userManager.AddToRoleAsync(admin, "Admin");

        var service = new UserService(userManager, new FakeTenantProvider(1));
        return (service, admin);
    }

    [Fact]
    public async Task CreateAsync_AddsUserToTenantWithRequestedRole()
    {
        var (service, _) = await SeedAsync();

        var result = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Staff Member",
            Email = "staff@test.com",
            Password = "Password123",
            Role = "Staff"
        });

        Assert.Contains("Staff", result.Roles);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        var (service, admin) = await SeedAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateUserRequest
        {
            FullName = "Duplicate",
            Email = admin.Email!,
            Password = "Password123",
            Role = "Staff"
        }));
    }

    [Fact]
    public async Task UpdateAsync_ChangesRole()
    {
        var (service, admin) = await SeedAsync();

        var staff = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Staff Member",
            Email = "staff2@test.com",
            Password = "Password123",
            Role = "Staff"
        });

        var updated = await service.UpdateAsync(
            staff.Id,
            new UpdateUserRequest { FullName = "Staff Member", Role = "Admin", IsActive = true },
            admin.Id);

        Assert.Contains("Admin", updated.Roles);
        Assert.DoesNotContain("Staff", updated.Roles);
    }

    [Fact]
    public async Task UpdateAsync_SelfDeactivation_ThrowsConflictException()
    {
        var (service, admin) = await SeedAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(
            admin.Id,
            new UpdateUserRequest { FullName = admin.FullName, Role = "Admin", IsActive = false },
            admin.Id));
    }

    [Fact]
    public async Task DeactivateAsync_SelfDeactivation_ThrowsConflictException()
    {
        var (service, admin) = await SeedAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(admin.Id, admin.Id));
    }

    [Fact]
    public async Task DeactivateAsync_OtherUser_Succeeds()
    {
        var (service, admin) = await SeedAsync();

        var staff = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Staff Member",
            Email = "staff3@test.com",
            Password = "Password123",
            Role = "Staff"
        });

        await service.DeactivateAsync(staff.Id, admin.Id);

        var reloaded = await service.GetByIdAsync(staff.Id);
        Assert.False(reloaded.IsActive);
    }
}
