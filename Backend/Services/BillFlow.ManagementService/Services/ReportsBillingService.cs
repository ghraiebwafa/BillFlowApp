using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Constants;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class ReportsBillingService(
    IReportsRepository reportsRepository,
    ICurrentUserAccessor currentUser) : IReportsBillingService
{
    public async Task<OperationResult<ReportExportFile>> ExportSalesAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<ReportExportFile>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (!TryValidateDateRange(from, to, out var dateError))
            return OperationResult<ReportExportFile>.Fail(dateError!, StatusCodes.Status400BadRequest);

        var rows = await reportsRepository.GetSalesAsync(ownerId.Value!.Value, from, to, cancellationToken);

        var export = ReportExporter.Export(
            "sales-report",
            "Sales",
            ["Invoice Number", "Client", "Invoice Date", "Due Date", "Status", "Subtotal", "Tax", "Total"],
            rows.Select(r => new[]
            {
                r.InvoiceNumber,
                r.ClientCompanyName,
                r.InvoiceDate.ToString("yyyy-MM-dd"),
                r.DueDate.ToString("yyyy-MM-dd"),
                r.Status.ToString(),
                r.Subtotal.ToString("0.00"),
                r.TaxAmount.ToString("0.00"),
                r.Total.ToString("0.00"),
            }).ToList(),
            format);

        return OperationResult<ReportExportFile>.Ok(ToFile(export));
    }

    public async Task<OperationResult<ReportExportFile>> ExportPaymentsAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<ReportExportFile>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (!TryValidateDateRange(from, to, out var dateError))
            return OperationResult<ReportExportFile>.Fail(dateError!, StatusCodes.Status400BadRequest);

        var rows = await reportsRepository.GetPaymentsAsync(ownerId.Value!.Value, from, to, cancellationToken);

        var export = ReportExporter.Export(
            "payments-report",
            "Payments",
            ["Payment Date", "Invoice Number", "Client", "Amount", "Method", "Status", "Reference"],
            rows.Select(r => new[]
            {
                r.PaymentDate.ToString("yyyy-MM-dd"),
                r.InvoiceNumber,
                r.ClientCompanyName,
                r.Amount.ToString("0.00"),
                r.Method.ToString(),
                r.Status.ToString(),
                r.Reference ?? string.Empty,
            }).ToList(),
            format);

        return OperationResult<ReportExportFile>.Ok(ToFile(export));
    }

    public async Task<OperationResult<ReportExportFile>> ExportOutstandingAsync(
        ReportFormat format,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<ReportExportFile>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        var rows = await reportsRepository.GetOutstandingAsync(ownerId.Value!.Value, cancellationToken);

        var export = ReportExporter.Export(
            "outstanding-report",
            "Outstanding",
            ["Invoice Number", "Client", "Due Date", "Status", "Total", "Paid", "Remaining"],
            rows.Select(r => new[]
            {
                r.InvoiceNumber,
                r.ClientCompanyName,
                r.DueDate.ToString("yyyy-MM-dd"),
                r.Status.ToString(),
                r.Total.ToString("0.00"),
                r.Paid.ToString("0.00"),
                r.Remaining.ToString("0.00"),
            }).ToList(),
            format);

        return OperationResult<ReportExportFile>.Ok(ToFile(export));
    }

    public async Task<OperationResult<ReportExportFile>> ExportTaxesAsync(
        ReportFormat format,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = RequireBusinessOwnerId<ReportExportFile>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (!TryValidateDateRange(from, to, out var dateError))
            return OperationResult<ReportExportFile>.Fail(dateError!, StatusCodes.Status400BadRequest);

        var rows = await reportsRepository.GetTaxesAsync(ownerId.Value!.Value, from, to, cancellationToken);

        var export = ReportExporter.Export(
            "taxes-report",
            "Taxes",
            ["Invoice Number", "Client", "Invoice Date", "Subtotal", "Tax Rate %", "Tax Amount", "Total"],
            rows.Select(r => new[]
            {
                r.InvoiceNumber,
                r.ClientCompanyName,
                r.InvoiceDate.ToString("yyyy-MM-dd"),
                r.Subtotal.ToString("0.00"),
                r.TaxRate.ToString("0.##"),
                r.TaxAmount.ToString("0.00"),
                r.Total.ToString("0.00"),
            }).ToList(),
            format);

        return OperationResult<ReportExportFile>.Ok(ToFile(export));
    }

    private static bool TryValidateDateRange(DateTime? from, DateTime? to, out string? error)
    {
        if (from is not null && to is not null && to.Value.Date < from.Value.Date)
        {
            error = "The 'to' date cannot be before the 'from' date.";
            return false;
        }

        error = null;
        return true;
    }

    private static ReportExportFile ToFile(ReportExportResult export) => new()
    {
        Content = export.Content,
        FileName = export.FileName,
        ContentType = export.ContentType,
    };

    private (Guid? Value, OperationResult<T>? Error) RequireBusinessOwnerId<T>()
    {
        if (!IsBusinessOwner())
        {
            return (null, OperationResult<T>.Fail(
                "Business owner role is required.",
                StatusCodes.Status403Forbidden));
        }

        if (currentUser.UserId is null)
        {
            return (null, OperationResult<T>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized));
        }

        return (currentUser.UserId, null);
    }

    private bool IsBusinessOwner() =>
        string.Equals(currentUser.Role, RoleNames.Visitor, StringComparison.OrdinalIgnoreCase);
}
