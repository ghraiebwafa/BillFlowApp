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
[Authorize(Policy = RoleNames.SuperAdmin)]
[Route("api/v1.0/management/Admin")]
public class AdminController(IAdminManagementService adminService) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        adminService.GetAllAsync(cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.BillingRead)]
    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        adminService.GetByIdAsync(id, cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateAdminRequest request,
        CancellationToken cancellationToken) =>
        adminService.CreateAsync(request, cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAdminRequest request,
        CancellationToken cancellationToken) =>
        adminService.UpdateAsync(id, request, cancellationToken).ToManagementActionResult();

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        adminService.DeleteAsync(id, cancellationToken).ToManagementActionResult();
}
