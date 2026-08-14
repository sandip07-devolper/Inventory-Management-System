using InventoryOrderSystem.API.DTOs.SalesOrders;

namespace InventoryOrderSystem.API.Services.SalesOrders;

public interface ISalesOrderService
{
    Task<IEnumerable<SalesOrderDto>> GetAllAsync();
    Task<SalesOrderDto> GetByIdAsync(int id);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request);
    Task<SalesOrderDto> UpdateAsync(int id, UpdateSalesOrderRequest request);
    Task DeleteAsync(int id);

    /// <summary>Validates stock availability, marks the order Fulfilled, and deducts stock.</summary>
    Task<SalesOrderDto> FulfillAsync(int id);

    /// <summary>Cancels a Draft order. Has no stock effect since nothing was deducted.</summary>
    Task<SalesOrderDto> CancelAsync(int id);
}
