using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.PurchaseOrders;
using InventoryOrderSystem.API.Services.PurchaseOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderDto>>> GetAll([FromQuery] PurchaseOrderQuery query)
        => Ok(await _purchaseOrderService.GetAllAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id)
        => Ok(await _purchaseOrderService.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderRequest request)
    {
        var order = await _purchaseOrderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Only allowed while the order is still Draft.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> Update(int id, UpdatePurchaseOrderRequest request)
        => Ok(await _purchaseOrderService.UpdateAsync(id, request));

    /// <summary>Only allowed while the order is still Draft.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _purchaseOrderService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Marks the order Received and adds its items' quantities to stock.</summary>
    [HttpPost("{id:int}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(int id)
        => Ok(await _purchaseOrderService.ReceiveAsync(id));

    /// <summary>Cancels a Draft order. No stock effect.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(int id)
        => Ok(await _purchaseOrderService.CancelAsync(id));
}
