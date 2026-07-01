using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public interface IAuditEventRepository
{
    Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> GetRecentByOwnerAsync(
        Guid ownerId,
        int limit,
        CancellationToken cancellationToken = default);
}
