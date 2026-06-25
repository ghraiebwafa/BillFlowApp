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
[Route("api/v1.0/billing/Item")]
public class ItemController(IItemBillingService itemService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        itemService.GetAllAsync(search, includeArchived, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        itemService.GetByIdAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateItemRequest request,
        CancellationToken cancellationToken) =>
        itemService.CreateAsync(request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateItemRequest request,
        CancellationToken cancellationToken) =>
        itemService.UpdateAsync(id, request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Archive/{id:guid}")]
    public Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        itemService.ArchiveAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        itemService.DeleteAsync(id, cancellationToken).ToBillingActionResult();
}
