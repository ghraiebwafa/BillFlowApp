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
[Route("api/v1.0/billing/Client")]
public class ClientController(IClientBillingService clientService) : ControllerBase
{
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        ToActionResult(clientService.GetAllAsync(search, cancellationToken));

    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(clientService.GetByIdAsync(id, cancellationToken));

    [EnableRateLimiting(RateLimitPolicies.AuthModerate)]
    [HttpPost("Create")]
    public Task<IActionResult> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(clientService.CreateAsync(request, cancellationToken));

    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(clientService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(clientService.DeleteAsync(id, cancellationToken));

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
