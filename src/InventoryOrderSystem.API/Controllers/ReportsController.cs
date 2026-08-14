using InventoryOrderSystem.API.DTOs.Reports;
using InventoryOrderSystem.API.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Active products whose stock has fallen to or below their reorder level.</summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<LowStockReportDto>> GetLowStock()
        => Ok(await _reportService.GetLowStockReportAsync());

    /// <summary>Current inventory value (cost and retail) across all products with stock on hand.</summary>
    [HttpGet("inventory-valuation")]
    public async Task<ActionResult<InventoryValuationReportDto>> GetInventoryValuation()
        => Ok(await _reportService.GetInventoryValuationReportAsync());
}
