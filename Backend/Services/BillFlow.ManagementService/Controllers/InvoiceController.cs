using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.Visitor)]
[Route("api/v1.0/billing/Invoice")]
public class InvoiceController(IInvoiceBillingService invoiceService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        invoiceService.GetAllAsync(status, search, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        invoiceService.GetByIdAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken) =>
        invoiceService.CreateAsync(request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken) =>
        invoiceService.UpdateAsync(id, request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Duplicate/{id:guid}")]
    public Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken) =>
        invoiceService.DuplicateAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        invoiceService.DeleteAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Send/{id:guid}")]
    public Task<IActionResult> Send(Guid id, CancellationToken cancellationToken) =>
        invoiceService.SendAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Email/{id:guid}")]
    public Task<IActionResult> Email(Guid id, CancellationToken cancellationToken) =>
        invoiceService.EmailInvoiceAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("MarkPaid/{id:guid}")]
    public Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken) =>
        invoiceService.MarkPaidAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Cancel/{id:guid}")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        invoiceService.CancelAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingExport)]
    [HttpGet("DownloadPdf/{id:guid}")]
    public Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken) =>
        this.ToBillingPdfResult(invoiceService.DownloadPdfAsync(id, cancellationToken));
}
