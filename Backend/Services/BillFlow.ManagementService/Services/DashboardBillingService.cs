using BillFlow.Models.Dtos.Billing;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class DashboardBillingService(
    IDashboardRepository dashboardRepository,
    ICurrentUserAccessor currentUser) : IDashboardBillingService
{
    public async Task<OperationResult<DashboardResponse>> GetSummaryAsync(
        int revenueMonths = 12,
        int topClientsLimit = 5,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<DashboardResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var owner = ownerId.Value!.Value;

        revenueMonths = Math.Clamp(revenueMonths, 1, 24);
        topClientsLimit = Math.Clamp(topClientsLimit, 1, 20);

        var dashboard = await dashboardRepository.GetDashboardAsync(
            owner,
            revenueMonths,
            topClientsLimit,
            cancellationToken);

        return OperationResult<DashboardResponse>.Ok(dashboard);
    }
}
