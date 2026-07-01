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
[Route("api/v1.0/billing/Activity")]
public class ActivityController(IAuditTrailService auditTrailService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetRecent")]
    public Task<IActionResult> GetRecent(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        auditTrailService.GetRecentAsync(limit, cancellationToken).ToBillingActionResult();
}
