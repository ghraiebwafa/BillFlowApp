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
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        ToActionResult(itemService.GetAllAsync(search, includeArchived, cancellationToken));

    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(itemService.GetByIdAsync(id, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateItemRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(itemService.CreateAsync(request, cancellationToken));

    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateItemRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(itemService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("Archive/{id:guid}")]
    public Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(itemService.ArchiveAsync(id, cancellationToken));

    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(itemService.DeleteAsync(id, cancellationToken));

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
