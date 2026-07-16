using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Route("api/v1.0/portal")]
public class PortalController(IPortalService portalService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.PortalRead)]
    [HttpGet("{token}")]
    public Task<IActionResult> GetInvoice(string token, CancellationToken cancellationToken) =>
        portalService.GetInvoiceByTokenAsync(token, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.PortalExport)]
    [HttpGet("{token}/pdf")]
    public Task<IActionResult> DownloadPdf(string token, CancellationToken cancellationToken) =>
        this.ToBillingPdfResult(portalService.DownloadPdfByTokenAsync(token, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{token}/checkout")]
    public Task<IActionResult> Checkout(string token, CancellationToken cancellationToken) =>
        portalService.CreateCheckoutAsync(token, cancellationToken).ToBillingActionResult();
}
