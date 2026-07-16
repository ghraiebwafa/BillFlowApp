using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public interface ICompanySettingsRepository
{
    Task<CompanySettings?> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<CompanySettings> UpsertAsync(CompanySettings settings, CancellationToken cancellationToken = default);

    Task<CompanySettings> SaveAsync(CompanySettings settings, CancellationToken cancellationToken = default);
}
