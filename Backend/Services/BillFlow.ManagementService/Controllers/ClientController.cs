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
[Route("api/v1.0/billing/Client")]
public class ClientController(IClientBillingService clientService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        clientService.GetAllAsync(search, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        clientService.GetByIdAsync(id, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken) =>
        clientService.CreateAsync(request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken) =>
        clientService.UpdateAsync(id, request, cancellationToken).ToBillingActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        clientService.DeleteAsync(id, cancellationToken).ToBillingActionResult();
}
