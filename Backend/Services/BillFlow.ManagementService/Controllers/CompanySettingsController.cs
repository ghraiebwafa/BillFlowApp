using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/company-settings")]
public class CompanySettingsController(ICompanySettingsBillingService companySettingsService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet]
    public Task<IActionResult> Get(CancellationToken cancellationToken) =>
        companySettingsService.GetAsync(cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut]
    public Task<IActionResult> Upsert(
        [FromBody] UpsertCompanySettingsRequest request,
        CancellationToken cancellationToken) =>
        companySettingsService.UpsertAsync(request, cancellationToken).ToBillingActionResult();
}
