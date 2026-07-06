using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Shared.Configuration;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/invoices")]
public class InvoiceController(
    IInvoiceBillingService invoiceService,
    IPaymentBillingService paymentService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        invoiceService.GetAllAsync(status, search, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        invoiceService.GetByIdAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("{invoiceId:guid}/payments")]
    public Task<IActionResult> GetPayments(Guid invoiceId, CancellationToken cancellationToken) =>
        paymentService.GetByInvoiceAsync(invoiceId, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken) =>
        invoiceService.CreateAsync(request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken) =>
        invoiceService.UpdateAsync(id, request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{id:guid}/duplicate")]
    public Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken) =>
        invoiceService.DuplicateAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        invoiceService.DeleteAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{id:guid}/send")]
    public Task<IActionResult> Send(Guid id, CancellationToken cancellationToken) =>
        invoiceService.SendAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{id:guid}/email")]
    public Task<IActionResult> Email(Guid id, CancellationToken cancellationToken) =>
        invoiceService.EmailInvoiceAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{id:guid}/mark-paid")]
    public Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken) =>
        invoiceService.MarkPaidAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        invoiceService.CancelAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingExport)]
    [HttpGet("{id:guid}/pdf")]
    public Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken) =>
        this.ToBillingPdfResult(invoiceService.DownloadPdfAsync(id, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("{id:guid}/share-link")]
    public Task<IActionResult> GenerateShareLink(Guid id, CancellationToken cancellationToken)
    {
        var baseUrl = BillFlowEnv.Get("PORTAL_BASE_URL", string.Empty);
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{Request.Scheme}://{Request.Host}";

        return invoiceService.GenerateShareLinkAsync(id, baseUrl, cancellationToken).ToBillingActionResult();
    }

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("{id:guid}/share-link")]
    public Task<IActionResult> RevokeShareLink(Guid id, CancellationToken cancellationToken) =>
        invoiceService.RevokeShareLinkAsync(id, cancellationToken).ToBillingActionResult();
}
