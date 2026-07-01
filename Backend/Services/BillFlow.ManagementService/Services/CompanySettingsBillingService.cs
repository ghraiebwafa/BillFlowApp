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
        CreatedAt = settings.CreatedAt,
        UpdatedAt = settings.UpdatedAt,
    };
}
