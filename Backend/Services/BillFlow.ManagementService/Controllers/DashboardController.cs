using BillFlow.ManagementService.Services;
using BillFlow.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/Dashboard")]
public class DashboardController(IDashboardBillingService dashboardService) : ControllerBase
{
    [HttpGet("GetSummary")]
    public Task<IActionResult> GetSummary(
        [FromQuery] int revenueMonths = 12,
        [FromQuery] int topClientsLimit = 5,
        CancellationToken cancellationToken = default) =>
        ToActionResult(dashboardService.GetSummaryAsync(revenueMonths, topClientsLimit, cancellationToken));

    private static async Task<IActionResult> ToActionResult<T>(Task<OperationResult<T>> task)
    {
        var result = await task;

        if (result.IsSuccess)
            return new ObjectResult(result.Value) { StatusCode = result.StatusCode };

        return new ObjectResult(new { title = "Error", detail = result.Error })
        {
            StatusCode = result.StatusCode,
        };
    }
}
