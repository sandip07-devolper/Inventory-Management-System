using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.Tests.TestHelpers;

public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a fresh, isolated in-memory AppDbContext with a fixed tenant
    /// context, so the tenant query filters and auto-stamping behave exactly
    /// as they would in production against a real database.
    /// </summary>
    public static AppDbContext Create(int tenantId = 1)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new FakeTenantProvider(tenantId));
    }
}
