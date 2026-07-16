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

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("logo")]
    [RequestSizeLimit(2_100_000)]
    public async Task<IActionResult> UploadLogo(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "Logo file is required." });

        await using var stream = file.OpenReadStream();
        return await companySettingsService
            .UploadLogoAsync(stream, file.ContentType, cancellationToken)
            .ToBillingActionResult();
    }

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("logo")]
    public Task<IActionResult> RemoveLogo(CancellationToken cancellationToken) =>
        companySettingsService.RemoveLogoAsync(cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
    {
        var logo = await companySettingsService.GetLogoAsync(cancellationToken);
        if (logo is null)
            return NotFound();

        return File(logo.Value.Bytes, logo.Value.ContentType);
    }
}
