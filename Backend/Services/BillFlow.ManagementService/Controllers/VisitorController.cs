using BillFlow.ManagementService.Extensions;
using BillFlow.ManagementService.Services;
using BillFlow.Models.Dtos.Management;
using BillFlow.Shared.Constants;
using BillFlow.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.AdminOrSuperAdmin)]
[Route("api/v1.0/management/visitors")]
public class VisitorController(IVisitorManagementService visitorService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        visitorService.GetAllAsync(cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        visitorService.GetByIdAsync(id, cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVisitorRequest request,
        CancellationToken cancellationToken) =>
        visitorService.UpdateAsync(id, request, cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        visitorService.DeleteAsync(id, cancellationToken).ToManagementActionResult();
}
