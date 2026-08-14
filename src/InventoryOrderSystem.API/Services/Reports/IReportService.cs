using InventoryOrderSystem.API.DTOs.Reports;

namespace InventoryOrderSystem.API.Services.Reports;

public interface IReportService
{
    Task<LowStockReportDto> GetLowStockReportAsync();
    Task<InventoryValuationReportDto> GetInventoryValuationReportAsync();
}
