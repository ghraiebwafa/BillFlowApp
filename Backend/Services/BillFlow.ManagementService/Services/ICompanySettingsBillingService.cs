using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface ICompanySettingsBillingService
{
    Task<OperationResult<CompanySettingsResponse>> GetAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<CompanySettingsResponse>> UpsertAsync(
        UpsertCompanySettingsRequest request,
        CancellationToken cancellationToken = default);
}
