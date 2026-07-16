using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class CompanySettingsBillingService(
    ICompanySettingsRepository companySettingsRepository,
    IAuditTrailService auditTrail,
    ICurrentUserAccessor currentUser) : ICompanySettingsBillingService
{
    public async Task<OperationResult<CompanySettingsResponse>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<CompanySettingsResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var settings = await companySettingsRepository.GetByOwnerAsync(ownerId.Value!.Value, cancellationToken);
        if (settings is null)
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Company settings have not been configured yet.",
                StatusCodes.Status404NotFound);
        }

        return OperationResult<CompanySettingsResponse>.Ok(Map(settings));
    }

    public async Task<OperationResult<CompanySettingsResponse>> UpsertAsync(
        UpsertCompanySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<CompanySettingsResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (!BillingInputValidator.TryValidateRequiredText(request.CompanyName, "Company name", out var nameError))
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                nameError!,
                StatusCodes.Status400BadRequest);
        }

        var prefix = request.InvoiceNumberPrefix.Trim().ToUpperInvariant();
        if (!BillingInputValidator.IsNonWhiteSpace(prefix))
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Invoice number prefix is required.",
                StatusCodes.Status400BadRequest);
        }

        var settings = new CompanySettings
        {
            OwnerId = ownerId.Value!.Value,
            CompanyName = request.CompanyName.Trim(),
            Address = request.Address?.Trim(),
            Country = request.Country?.Trim(),
            TaxNumber = request.TaxNumber?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            InvoiceNumberPrefix = prefix,
            DefaultTaxRate = request.DefaultTaxRate,
            PaymentTermsDays = request.PaymentTermsDays,
            TimeZone = request.TimeZone?.Trim(),
            BrandColor = NormalizeBrandColor(request.BrandColor),
            InvoiceFooterNote = request.InvoiceFooterNote?.Trim(),
            EnablePaymentReminders = request.EnablePaymentReminders,
            ReminderDaysBeforeDue = request.ReminderDaysBeforeDue,
        };

        var saved = await companySettingsRepository.UpsertAsync(settings, cancellationToken);
        await auditTrail.LogAsync(
            ownerId.Value!.Value,
            AuditAction.SettingsUpdated,
            AuditEntityType.CompanySettings,
            saved.OwnerId,
            $"Company settings updated for \"{saved.CompanyName}\".",
            cancellationToken);
        return OperationResult<CompanySettingsResponse>.Ok(Map(saved), StatusCodes.Status200OK);
    }

    public async Task<OperationResult<CompanySettingsResponse>> UploadLogoAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<CompanySettingsResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var settings = await companySettingsRepository.GetByOwnerAsync(ownerId.Value!.Value, cancellationToken);
        if (settings is null)
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Company settings have not been configured yet.",
                StatusCodes.Status404NotFound);
        }

        if (!IsAllowedLogoContentType(contentType))
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Logo must be a PNG, JPEG, or WebP image.",
                StatusCodes.Status400BadRequest);
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length is 0 or > 2_000_000)
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Logo must be between 1 byte and 2 MB.",
                StatusCodes.Status400BadRequest);
        }

        var bytes = buffer.ToArray();
        if (!LooksLikeAllowedImage(bytes, contentType))
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Logo file content does not match a PNG, JPEG, or WebP image.",
                StatusCodes.Status400BadRequest);
        }

        settings.LogoBytes = bytes;
        settings.LogoContentType = contentType.Split(';')[0].Trim().ToLowerInvariant();
        var saved = await companySettingsRepository.SaveAsync(settings, cancellationToken);

        return OperationResult<CompanySettingsResponse>.Ok(Map(saved));
    }

    public async Task<OperationResult<CompanySettingsResponse>> RemoveLogoAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<CompanySettingsResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var settings = await companySettingsRepository.GetByOwnerAsync(ownerId.Value!.Value, cancellationToken);
        if (settings is null)
        {
            return OperationResult<CompanySettingsResponse>.Fail(
                "Company settings have not been configured yet.",
                StatusCodes.Status404NotFound);
        }

        settings.LogoBytes = null;
        settings.LogoContentType = null;
        var saved = await companySettingsRepository.SaveAsync(settings, cancellationToken);
        return OperationResult<CompanySettingsResponse>.Ok(Map(saved));
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetLogoAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<(byte[] Bytes, string ContentType)?>(currentUser);
        if (ownerId.Error is not null)
            return null;

        var settings = await companySettingsRepository.GetByOwnerAsync(ownerId.Value!.Value, cancellationToken);
        if (settings?.LogoBytes is not { Length: > 0 })
            return null;

        return (settings.LogoBytes, settings.LogoContentType ?? "application/octet-stream");
    }

    private static bool IsAllowedLogoContentType(string contentType)
    {
        var type = contentType.Split(';')[0].Trim().ToLowerInvariant();
        return type is "image/png" or "image/jpeg" or "image/jpg" or "image/webp";
    }

    private static bool LooksLikeAllowedImage(byte[] bytes, string contentType)
    {
        var type = contentType.Split(';')[0].Trim().ToLowerInvariant();
        return type switch
        {
            "image/png" => bytes.Length >= 8
                && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
            "image/jpeg" or "image/jpg" => bytes.Length >= 3
                && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/webp" => bytes.Length >= 12
                && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P',
            _ => false,
        };
    }

    internal static CompanySettingsResponse Map(CompanySettings settings) => new()
    {
        CompanyName = settings.CompanyName,
        Address = settings.Address,
        Country = settings.Country,
        TaxNumber = settings.TaxNumber,
        PhoneNumber = settings.PhoneNumber,
        Email = settings.Email,
        Currency = settings.Currency,
        InvoiceNumberPrefix = settings.InvoiceNumberPrefix,
        DefaultTaxRate = settings.DefaultTaxRate,
        PaymentTermsDays = settings.PaymentTermsDays,
        TimeZone = settings.TimeZone,
        BrandColor = settings.BrandColor,
        InvoiceFooterNote = settings.InvoiceFooterNote,
        HasLogo = settings.LogoBytes is { Length: > 0 },
        EnablePaymentReminders = settings.EnablePaymentReminders,
        ReminderDaysBeforeDue = settings.ReminderDaysBeforeDue,
        LogoBytes = settings.LogoBytes,
        CreatedAt = settings.CreatedAt,
        UpdatedAt = settings.UpdatedAt,
    };

    private static string? NormalizeBrandColor(string? brandColor)
    {
        if (string.IsNullOrWhiteSpace(brandColor))
            return null;

        var value = brandColor.Trim();
        if (!value.StartsWith('#'))
            value = $"#{value}";

        return value.Length == 7 ? value.ToUpperInvariant() : null;
    }
}
