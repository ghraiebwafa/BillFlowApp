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
    [HttpGet("GetByInvoice/{invoiceId:guid}")]
    public Task<IActionResult> GetByInvoice(Guid invoiceId, CancellationToken cancellationToken) =>
        ToActionResult(paymentService.GetByInvoiceAsync(invoiceId, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(paymentService.CreateAsync(request, cancellationToken));

    [HttpPost("Refund/{id:guid}")]
    public Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(paymentService.RefundAsync(id, cancellationToken));

    [HttpPost("Cancel/{id:guid}")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(paymentService.CancelAsync(id, cancellationToken));

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
