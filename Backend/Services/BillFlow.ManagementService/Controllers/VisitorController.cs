using BillFlow.ManagementService.Services;
using BillFlow.Models.Dtos.Management;
using BillFlow.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.ManagementService.Controllers;

[ApiController]
[Authorize(Policy = RoleNames.AdminOrSuperAdmin)]
[Route("api/v1.0/management/Visitor")]
public class VisitorController(IVisitorManagementService visitorService) : ControllerBase
{
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ToActionResult(visitorService.GetAllAsync(cancellationToken));

    [HttpGet("GetById/{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(visitorService.GetByIdAsync(id, cancellationToken));

    [HttpPut("Update/{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVisitorRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(visitorService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(visitorService.DeleteAsync(id, cancellationToken));

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
