using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryOrderSystem.Tests.TestHelpers;

public static class IdentityTestFactory
{
    public static (AppDbContext DbContext, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole<int>> RoleManager)
        Create(int tenantId = 1)
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddSingleton<ITenantProvider>(new FakeTenantProvider(tenantId));
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();

        return (
            provider.GetRequiredService<AppDbContext>(),
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<IdentityRole<int>>>()
        );
    }
}
