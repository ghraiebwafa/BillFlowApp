using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class PortalService(
    IInvoiceShareTokenRepository shareTokenRepository,
    ICompanySettingsRepository companySettingsRepository,
    IInvoicePdfGenerator pdfGenerator,
    IAuditTrailService auditTrail) : IPortalService
{
    public async Task<OperationResult<PublicInvoiceResponse>> GetInvoiceByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var shareToken = await shareTokenRepository.GetByTokenAsync(token, cancellationToken);
        var validation = ValidateToken(shareToken);
        if (validation is not null)
            return OperationResult<PublicInvoiceResponse>.Fail(validation, StatusCodes.Status404NotFound);

        var invoice = shareToken!.Invoice;
        var settings = await companySettingsRepository.GetByOwnerAsync(invoice.OwnerId, cancellationToken);

        await auditTrail.LogAnonymousAsync(
            invoice.OwnerId,
            AuditAction.PortalViewed,
            AuditEntityType.Invoice,
            invoice.Id,
            $"Invoice {invoice.InvoiceNumber} viewed via customer portal.",
            cancellationToken);

        return OperationResult<PublicInvoiceResponse>.Ok(MapPublic(invoice, settings));
    }

    public async Task<OperationResult<InvoicePdfFile>> DownloadPdfByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var shareToken = await shareTokenRepository.GetByTokenAsync(token, cancellationToken);
        var validation = ValidateToken(shareToken);
        if (validation is not null)
            return OperationResult<InvoicePdfFile>.Fail(validation, StatusCodes.Status404NotFound);

        var invoice = shareToken!.Invoice;
        var settings = await companySettingsRepository.GetByOwnerAsync(invoice.OwnerId, cancellationToken);

        var detail = MapDetailForPortalPdf(invoice);
        var issuer = settings is null ? null : CompanySettingsBillingService.Map(settings);
        var content = pdfGenerator.Generate(detail, issuer);

        await auditTrail.LogAnonymousAsync(
            invoice.OwnerId,
            AuditAction.PortalPdfDownloaded,
            AuditEntityType.Invoice,
            invoice.Id,
            $"Invoice {invoice.InvoiceNumber} PDF downloaded via customer portal.",
            cancellationToken);

        return OperationResult<InvoicePdfFile>.Ok(new InvoicePdfFile
        {
            Content = content,
            FileName = $"{SanitizeFileName(invoice.InvoiceNumber)}.pdf",
        });
    }

    private static string? ValidateToken(InvoiceShareToken? shareToken)
    {
        if (shareToken is null)
            return "Invoice not found or link has expired.";

        if (shareToken.ExpiresAt.HasValue && shareToken.ExpiresAt.Value < DateTime.UtcNow)
            return "Invoice not found or link has expired.";

        if (shareToken.Invoice is null || shareToken.Invoice.IsDeleted)
            return "Invoice not found or link has expired.";

        if (shareToken.Invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return "Invoice not found or link has expired.";

        return null;
    }

    private static PublicInvoiceResponse MapPublic(Invoice invoice, CompanySettings? settings) => new()
    {
        InvoiceNumber = invoice.InvoiceNumber,
        Status = invoice.Status,
        ClientCompanyName = invoice.Client.CompanyName,
        ClientContactName = invoice.Client.ContactName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Subtotal = invoice.Subtotal,
        TaxRate = invoice.TaxRate,
        TaxAmount = invoice.TaxAmount,
        Total = invoice.Total,
        Notes = invoice.Notes,
        LineItems = invoice.LineItems
            .OrderBy(l => l.SortOrder)
            .Select(l => new InvoiceLineItemResponse
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                SortOrder = l.SortOrder,
            })
            .ToList(),
        Issuer = settings is null ? null : new PublicIssuerInfo
        {
            CompanyName = settings.CompanyName,
            Address = settings.Address,
            Country = settings.Country,
            PhoneNumber = settings.PhoneNumber,
            Email = settings.Email,
            TaxNumber = settings.TaxNumber,
            Currency = settings.Currency,
            BrandColor = SanitizeBrandColor(settings.BrandColor),
            InvoiceFooterNote = settings.InvoiceFooterNote,
        },
    };

    private static InvoiceDetailResponse MapDetailForPortalPdf(Invoice invoice)
    {
        var detail = MapDetail(invoice);
        detail.ClientEmail = string.Empty;
        return detail;
    }

    private static InvoiceDetailResponse MapDetail(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Status = invoice.Status,
        ClientId = invoice.ClientId,
        ClientCompanyName = invoice.Client.CompanyName,
        ClientContactName = invoice.Client.ContactName,
        ClientEmail = invoice.Client.Email,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Subtotal = invoice.Subtotal,
        TaxRate = invoice.TaxRate,
        TaxAmount = invoice.TaxAmount,
        Total = invoice.Total,
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        UpdatedAt = invoice.UpdatedAt,
        LineItems = invoice.LineItems
            .OrderBy(l => l.SortOrder)
            .Select(l => new InvoiceLineItemResponse
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                SortOrder = l.SortOrder,
            })
            .ToList(),
    };

    private static string? SanitizeBrandColor(string? brandColor)
    {
        if (string.IsNullOrWhiteSpace(brandColor))
            return null;

        var value = brandColor.Trim();
        if (!value.StartsWith('#'))
            value = $"#{value}";

        return value.Length == 7 && value[1..].All(c => char.IsAsciiHexDigit(c))
            ? value.ToUpperInvariant()
            : null;
    }

    private static string SanitizeFileName(string invoiceNumber)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(invoiceNumber.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }
}
