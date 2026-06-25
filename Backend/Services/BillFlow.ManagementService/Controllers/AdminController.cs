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
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ToActionResult(adminService.GetAllAsync(cancellationToken));

    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(adminService.GetByIdAsync(id, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateAdminRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(adminService.CreateAsync(request, cancellationToken));

    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAdminRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(adminService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(adminService.DeleteAsync(id, cancellationToken));

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
