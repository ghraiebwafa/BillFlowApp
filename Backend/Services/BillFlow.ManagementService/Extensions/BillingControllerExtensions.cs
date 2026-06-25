using BillFlow.ManagementService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.ManagementService.Extensions;

public static class BillingControllerExtensions
{
    public static async Task<IActionResult> ToBillingActionResult<T>(this Task<OperationResult<T>> task)
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

    public static async Task<IActionResult> ToBillingFileResult(
        this ControllerBase controller,
        Task<OperationResult<Models.Dtos.Billing.ReportExportFile>> task)
    {
        var result = await task;

        if (!result.IsSuccess)
        {
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

        return controller.File(
            result.Value!.Content,
            result.Value.ContentType,
            result.Value.FileName);
    }

    public static async Task<IActionResult> ToBillingPdfResult(
        this ControllerBase controller,
        Task<OperationResult<Models.Dtos.Billing.InvoicePdfFile>> task)
    {
        var result = await task;

        if (!result.IsSuccess)
        {
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

        return controller.File(
            result.Value!.Content,
            "application/pdf",
            result.Value.FileName);
    }
}
