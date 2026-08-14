using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.PurchaseOrders;

namespace InventoryOrderSystem.API.Services.PurchaseOrders;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderDto>> GetAllAsync(PurchaseOrderQuery query);
    Task<PurchaseOrderDto> GetByIdAsync(int id);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request);
    Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderRequest request);
    Task DeleteAsync(int id);

    /// <summary>Marks the order Received and adds its item quantities to product stock.</summary>
    Task<PurchaseOrderDto> ReceiveAsync(int id);

    /// <summary>Cancels a Draft order. Has no stock effect since nothing was received.</summary>
    Task<PurchaseOrderDto> CancelAsync(int id);
}
