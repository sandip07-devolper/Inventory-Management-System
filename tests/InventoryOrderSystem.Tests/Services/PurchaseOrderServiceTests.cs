using InventoryOrderSystem.API.DTOs.PurchaseOrders;
using InventoryOrderSystem.API.Services.PurchaseOrders;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryOrderSystem.Tests.Services;

public class PurchaseOrderServiceTests
{
    private static async Task<(AppDbContext Db, Supplier Supplier, Product Product)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();

        var category = new Category { Name = "Electronics" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Sku = "SKU-001",
            Name = "Widget",
            CategoryId = category.Id,
            UnitPrice = 10m,
            CostPrice = 5m,
            QuantityOnHand = 0
        };
        db.Products.Add(product);

        var supplier = new Supplier { Name = "Acme Supplies" };
        db.Suppliers.Add(supplier);

        await db.SaveChangesAsync();

        return (db, supplier, product);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesDraftOrderWithCorrectTotal()
    {
        var (db, supplier, product) = await SeedAsync();
        var service = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);

        var result = await service.CreateAsync(new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Items = new List<PurchaseOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 10, UnitCost = 4.5m }
            }
        });

        Assert.Equal("Draft", result.Status);
        Assert.Equal(45m, result.TotalAmount);
        Assert.StartsWith("PO-", result.OrderNumber);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentSupplier_ThrowsNotFoundException()
    {
        var (db, _, product) = await SeedAsync();
        var service = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = 9999,
            Items = new List<PurchaseOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 1, UnitCost = 1m }
            }
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task ReceiveAsync_WhenDraft_IncreasesProductStockAndMarksReceived()
    {
        var (db, supplier, product) = await SeedAsync();
        var service = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);

        var created = await service.CreateAsync(new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Items = new List<PurchaseOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 20, UnitCost = 3m }
            }
        });

        var received = await service.ReceiveAsync(created.Id);

        Assert.Equal("Received", received.Status);
        Assert.NotNull(received.ReceivedDate);

        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(20, updatedProduct!.QuantityOnHand);
    }

    [Fact]
    public async Task ReceiveAsync_WhenAlreadyReceived_ThrowsConflictException()
    {
        var (db, supplier, product) = await SeedAsync();
        var service = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);

        var created = await service.CreateAsync(new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Items = new List<PurchaseOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 5, UnitCost = 2m }
            }
        });

        await service.ReceiveAsync(created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.ReceiveAsync(created.Id));
    }

    [Fact]
    public async Task CancelAsync_WhenDraft_MarksCancelled()
    {
        var (db, supplier, product) = await SeedAsync();
        var service = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);

        var created = await service.CreateAsync(new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Items = new List<PurchaseOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 5, UnitCost = 2m }
            }
        });

        var cancelled = await service.CancelAsync(created.Id);

        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task DeleteAsync_WhenReceived_ThrowsConflictException()
    {
        var (db, supplier, product) = await SeedAsync();
        var service = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);

        var created = await service.CreateAsync(new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Items = new List<PurchaseOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 5, UnitCost = 2m }
            }
        });

        await service.ReceiveAsync(created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(created.Id));
    }
}
