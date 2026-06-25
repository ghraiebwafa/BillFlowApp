using BillFlow.Models.Dtos.Billing;

namespace BillFlow.ManagementService.Services;

public interface IDashboardBillingService
{
    Task<OperationResult<DashboardResponse>> GetSummaryAsync(
        int revenueMonths = 12,
        int topClientsLimit = 5,
        CancellationToken cancellationToken = default);
}
