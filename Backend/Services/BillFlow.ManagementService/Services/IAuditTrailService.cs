using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;

namespace BillFlow.ManagementService.Services;

public interface IAuditTrailService
{
    Task LogAsync(
        Guid ownerId,
        AuditAction action,
        AuditEntityType entityType,
        Guid entityId,
        string summary,
        CancellationToken cancellationToken = default);

    Task LogAnonymousAsync(
        Guid ownerId,
        AuditAction action,
        AuditEntityType entityType,
        Guid entityId,
        string summary,
        CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyList<AuditEventResponse>>> GetRecentAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);
}
