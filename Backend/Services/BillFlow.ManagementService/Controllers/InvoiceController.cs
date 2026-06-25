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
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.GetAllAsync(status, search, cancellationToken));

    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.GetByIdAsync(id, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.CreateAsync(request, cancellationToken));

    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("Duplicate/{id:guid}")]
    public Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.DuplicateAsync(id, cancellationToken));

    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.DeleteAsync(id, cancellationToken));

    [HttpPost("Send/{id:guid}")]
    public Task<IActionResult> Send(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.SendAsync(id, cancellationToken));

    [HttpPost("MarkPaid/{id:guid}")]
    public Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.MarkPaidAsync(id, cancellationToken));

    [HttpPost("Cancel/{id:guid}")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(invoiceService.CancelAsync(id, cancellationToken));

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
