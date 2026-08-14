using InventoryOrderSystem.API.DTOs.SalesOrders;
using InventoryOrderSystem.API.Services.SalesOrders;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Domain.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryOrderSystem.Tests.Services;

public class SalesOrderServiceTests
{
    private static async Task<(AppDbContext Db, Customer Customer, Product Product)> SeedAsync(int initialStock = 0)
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
            QuantityOnHand = initialStock
        };
        db.Products.Add(product);

        var customer = new Customer { Name = "Jane Doe" };
        db.Customers.Add(customer);

        await db.SaveChangesAsync();

        return (db, customer, product);
    }

    [Fact]
    public async Task FulfillAsync_WithSufficientStock_DeductsStockAndMarksFulfilled()
    {
        var (db, customer, product) = await SeedAsync(initialStock: 15);
        var service = new SalesOrderService(db, NullLogger<SalesOrderService>.Instance);

        var created = await service.CreateAsync(new CreateSalesOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<SalesOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 10, UnitPrice = 12m }
            }
        });

        var fulfilled = await service.FulfillAsync(created.Id);

        Assert.Equal("Fulfilled", fulfilled.Status);
        Assert.NotNull(fulfilled.FulfilledDate);

        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(5, updatedProduct!.QuantityOnHand);
    }

    [Fact]
    public async Task FulfillAsync_WithInsufficientStock_ThrowsConflictAndLeavesStockUnchanged()
    {
        var (db, customer, product) = await SeedAsync(initialStock: 3);
        var service = new SalesOrderService(db, NullLogger<SalesOrderService>.Instance);

        var created = await service.CreateAsync(new CreateSalesOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<SalesOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 10, UnitPrice = 12m }
            }
        });

        await Assert.ThrowsAsync<ConflictException>(() => service.FulfillAsync(created.Id));

        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(3, updatedProduct!.QuantityOnHand); // stock must remain untouched
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentProduct_ThrowsNotFoundException()
    {
        var (db, customer, _) = await SeedAsync();
        var service = new SalesOrderService(db, NullLogger<SalesOrderService>.Instance);

        var request = new CreateSalesOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<SalesOrderItemRequest>
            {
                new() { ProductId = 9999, Quantity = 1, UnitPrice = 1m }
            }
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CancelAsync_WhenDraft_MarksCancelled()
    {
        var (db, customer, product) = await SeedAsync(initialStock: 5);
        var service = new SalesOrderService(db, NullLogger<SalesOrderService>.Instance);

        var created = await service.CreateAsync(new CreateSalesOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<SalesOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 2, UnitPrice = 5m }
            }
        });

        var cancelled = await service.CancelAsync(created.Id);

        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task FulfillAsync_WhenAlreadyCancelled_ThrowsConflictException()
    {
        var (db, customer, product) = await SeedAsync(initialStock: 5);
        var service = new SalesOrderService(db, NullLogger<SalesOrderService>.Instance);

        var created = await service.CreateAsync(new CreateSalesOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<SalesOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 2, UnitPrice = 5m }
            }
        });

        await service.CancelAsync(created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.FulfillAsync(created.Id));
    }
}
