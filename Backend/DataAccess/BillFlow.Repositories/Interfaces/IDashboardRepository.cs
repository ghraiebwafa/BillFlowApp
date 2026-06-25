using BillFlow.Models.Dtos.Billing;

namespace BillFlow.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboardAsync(
        Guid ownerId,
        int revenueMonths = 12,
        int topClientsLimit = 5,
        CancellationToken cancellationToken = default);
}
