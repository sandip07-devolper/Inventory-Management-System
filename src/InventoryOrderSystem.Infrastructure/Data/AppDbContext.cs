using System.Reflection;
using InventoryOrderSystem.Domain.Common;
using InventoryOrderSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>().HasIndex(t => t.Slug).IsUnique();

        builder.Entity<ApplicationUser>(b =>
        {
            b.HasOne(u => u.Tenant)
                .WithMany()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasQueryFilter(u => u.TenantId == _tenantProvider.GetTenantId());
        });

        builder.Entity<Category>(b =>
        {
            b.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();
        });

        builder.Entity<Product>(b =>
        {
            b.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
            b.Property(p => p.UnitPrice).HasPrecision(18, 2);
            b.Property(p => p.CostPrice).HasPrecision(18, 2);

            b.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Supplier>(b =>
        {
            b.HasIndex(s => new { s.TenantId, s.Name }).IsUnique();
        });

        builder.Entity<PurchaseOrder>(b =>
        {
            b.HasIndex(p => new { p.TenantId, p.OrderNumber }).IsUnique();
            b.Property(p => p.TotalAmount).HasPrecision(18, 2);
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

            b.HasOne(p => p.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseOrderItem>(b =>
        {
            b.Property(i => i.UnitCost).HasPrecision(18, 2);

            b.HasOne(i => i.PurchaseOrder)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Customer>(b =>
        {
            b.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();
        });

        builder.Entity<SalesOrder>(b =>
        {
            b.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            b.HasOne(o => o.Customer)
                .WithMany(c => c.SalesOrders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalesOrderItem>(b =>
        {
            b.Property(i => i.UnitPrice).HasPrecision(18, 2);

            b.HasOne(i => i.SalesOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ApplyTenantQueryFilters(builder);
    }

    /// <summary>
    /// Automatically applies a "WHERE TenantId = @current" global query filter
    /// to every entity that implements ITenantEntity, so individual DbSets never
    /// need to remember to filter by tenant manually.
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, new object[] { builder });
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantProvider.GetTenantId());
    }

    public override int SaveChanges()
    {
        ApplyAuditAndTenantStamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndTenantStamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditAndTenantStamps()
    {
        var tenantId = _tenantProvider.GetTenantId();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ITenantEntity tenantEntity && tenantId.HasValue)
                    tenantEntity.TenantId = tenantId.Value;

                if (entry.Entity is BaseEntity added)
                    added.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified && entry.Entity is BaseEntity modified)
            {
                modified.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
