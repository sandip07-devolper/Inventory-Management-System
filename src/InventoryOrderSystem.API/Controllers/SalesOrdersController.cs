using InventoryOrderSystem.API.DTOs.Common;
using InventoryOrderSystem.API.DTOs.SalesOrders;
using InventoryOrderSystem.API.Services.SalesOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public SalesOrdersController(ISalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesOrderDto>>> GetAll([FromQuery] SalesOrderQuery query)
        => Ok(await _salesOrderService.GetAllAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalesOrderDto>> GetById(int id)
        => Ok(await _salesOrderService.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create(CreateSalesOrderRequest request)
    {
        var order = await _salesOrderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Only allowed while the order is still Draft.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SalesOrderDto>> Update(int id, UpdateSalesOrderRequest request)
        => Ok(await _salesOrderService.UpdateAsync(id, request));

    /// <summary>Only allowed while the order is still Draft.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _salesOrderService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Validates stock, marks the order Fulfilled, and deducts stock.</summary>
    [HttpPost("{id:int}/fulfill")]
    public async Task<ActionResult<SalesOrderDto>> Fulfill(int id)
        => Ok(await _salesOrderService.FulfillAsync(id));

    /// <summary>Cancels a Draft order. No stock effect.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<SalesOrderDto>> Cancel(int id)
        => Ok(await _salesOrderService.CancelAsync(id));
}
