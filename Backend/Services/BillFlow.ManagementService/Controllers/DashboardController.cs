using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/Dashboard")]
public class DashboardController(IDashboardBillingService dashboardService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetSummary")]
    public Task<IActionResult> GetSummary(
        [FromQuery] int revenueMonths = 12,
        [FromQuery] int topClientsLimit = 5,
        CancellationToken cancellationToken = default) =>
        dashboardService.GetSummaryAsync(revenueMonths, topClientsLimit, cancellationToken).ToBillingActionResult();
}
