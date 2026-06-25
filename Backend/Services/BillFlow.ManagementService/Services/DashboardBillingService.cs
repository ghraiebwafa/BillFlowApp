using BillFlow.Models.Dtos.Billing;
using BillFlow.Repositories.Interfaces;
using BillFlow.Shared.Constants;

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
        var ownerId = RequireBusinessOwnerId<DashboardResponse>();
        if (ownerId.Error is not null)
            return ownerId.Error;

        var dashboard = await dashboardRepository.GetDashboardAsync(
            ownerId.Value!.Value,
            revenueMonths,
            topClientsLimit,
            cancellationToken);

        return OperationResult<DashboardResponse>.Ok(dashboard);
    }

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
