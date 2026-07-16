using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface ICompanySettingsBillingService
{
    Task<OperationResult<CompanySettingsResponse>> GetAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<CompanySettingsResponse>> UpsertAsync(
        UpsertCompanySettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CompanySettingsResponse>> UploadLogoAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CompanySettingsResponse>> RemoveLogoAsync(
        CancellationToken cancellationToken = default);

    Task<(byte[] Bytes, string ContentType)?> GetLogoAsync(CancellationToken cancellationToken = default);
}
