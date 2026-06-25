using BillFlow.ManagementService.Services;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/Reports")]
public class ReportsController(IReportsBillingService reportsService) : ControllerBase
{
    [HttpGet("ExportSales")]
    public Task<IActionResult> ExportSales(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        ToFileResult(reportsService.ExportSalesAsync(format, from, to, cancellationToken));

    [HttpGet("ExportPayments")]
    public Task<IActionResult> ExportPayments(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        ToFileResult(reportsService.ExportPaymentsAsync(format, from, to, cancellationToken));

    [HttpGet("ExportOutstanding")]
    public Task<IActionResult> ExportOutstanding(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        CancellationToken cancellationToken = default) =>
        ToFileResult(reportsService.ExportOutstandingAsync(format, cancellationToken));

    [HttpGet("ExportTaxes")]
    public Task<IActionResult> ExportTaxes(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        ToFileResult(reportsService.ExportTaxesAsync(format, from, to, cancellationToken));

    private async Task<IActionResult> ToFileResult(Task<OperationResult<ReportExportFile>> task)
    {
        var result = await task;

        if (!result.IsSuccess)
        {
            return new ObjectResult(new { title = "Error", detail = result.Error })
            {
                StatusCode = result.StatusCode,
            };
        }

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
}
