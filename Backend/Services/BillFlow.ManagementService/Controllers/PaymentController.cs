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
[Route("api/v1.0/billing/Payment")]
public class PaymentController(IPaymentBillingService paymentService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        paymentService.GetAllAsync(cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetByInvoice/{invoiceId:guid}")]
    public Task<IActionResult> GetByInvoice(Guid invoiceId, CancellationToken cancellationToken) =>
        paymentService.GetByInvoiceAsync(invoiceId, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken) =>
        paymentService.CreateAsync(request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Refund/{id:guid}")]
    public Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken) =>
        paymentService.RefundAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Cancel/{id:guid}")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        paymentService.CancelAsync(id, cancellationToken).ToBillingActionResult();
}
