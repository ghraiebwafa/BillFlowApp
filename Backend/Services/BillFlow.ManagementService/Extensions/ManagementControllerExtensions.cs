using BillFlow.ManagementService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.ManagementService.Extensions;

public static class ManagementControllerExtensions
{
    public static async Task<IActionResult> ToManagementActionResult<T>(this Task<OperationResult<T>> task)
    {
        var result = await task;

        if (result.IsSuccess)
            return new ObjectResult(result.Value) { StatusCode = result.StatusCode };

        return new ObjectResult(new ProblemDetails
        {
            Title = "Error",
            Detail = result.Error,
            Status = result.StatusCode,
        })
        {
            StatusCode = result.StatusCode,
        };
    }
}
