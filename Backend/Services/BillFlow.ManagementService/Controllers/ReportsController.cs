using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Models.Shared.Enums;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/Reports")]
public class ReportsController(IReportsBillingService reportsService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingExport)]
    [HttpGet("ExportSales")]
    public Task<IActionResult> ExportSales(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        this.ToBillingFileResult(reportsService.ExportSalesAsync(format, from, to, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.BillingExport)]
    [HttpGet("ExportPayments")]
    public Task<IActionResult> ExportPayments(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        this.ToBillingFileResult(reportsService.ExportPaymentsAsync(format, from, to, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.BillingExport)]
    [HttpGet("ExportOutstanding")]
    public Task<IActionResult> ExportOutstanding(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        CancellationToken cancellationToken = default) =>
        this.ToBillingFileResult(reportsService.ExportOutstandingAsync(format, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.BillingExport)]
    [HttpGet("ExportTaxes")]
    public Task<IActionResult> ExportTaxes(
        [FromQuery] ReportFormat format = ReportFormat.Csv,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default) =>
        this.ToBillingFileResult(reportsService.ExportTaxesAsync(format, from, to, cancellationToken));
}
