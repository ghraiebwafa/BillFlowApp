using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Models.Shared.Enums;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class AuditTrailService(
    IAuditEventRepository auditEventRepository,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUser) : IAuditTrailService
{
    private const int MaxLimit = 100;

    public async Task LogAsync(
        Guid ownerId,
        AuditAction action,
        AuditEntityType entityType,
        Guid entityId,
        string summary,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = currentUser.UserId;
        if (actorUserId is null)
            return;

        try
        {
            var actor = await userRepository.GetByIdAsync(actorUserId.Value, cancellationToken);
            var displayName = actor?.FullName ?? "Unknown user";

            await auditEventRepository.RecordAsync(
                new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    OwnerId = ownerId,
                    ActorUserId = actorUserId.Value,
                    ActorDisplayName = displayName,
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    Summary = summary.Trim(),
                },
                cancellationToken);
        }
        catch
        {
            // Audit logging must not break billing operations.
        }
    }

    public async Task LogAnonymousAsync(
        Guid ownerId,
        AuditAction action,
        AuditEntityType entityType,
        Guid entityId,
        string summary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await auditEventRepository.RecordAsync(
                new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    OwnerId = ownerId,
                    ActorUserId = Guid.Empty,
                    ActorDisplayName = "Customer portal",
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    Summary = summary.Trim(),
                },
                cancellationToken);
        }
        catch
        {
            // Audit logging must not break portal operations.
        }
    }

    public async Task<OperationResult<IReadOnlyList<AuditEventResponse>>> GetRecentAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<IReadOnlyList<AuditEventResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var safeLimit = Math.Clamp(limit, 1, MaxLimit);
        var events = await auditEventRepository.GetRecentByOwnerAsync(
            ownerId.Value!.Value,
            safeLimit,
            cancellationToken);

        return OperationResult<IReadOnlyList<AuditEventResponse>>.Ok(
            events.Select(Map).ToList());
    }

    private static AuditEventResponse Map(AuditEvent auditEvent) => new()
    {
        Id = auditEvent.Id,
        ActorUserId = auditEvent.ActorUserId,
        ActorDisplayName = auditEvent.ActorDisplayName,
        EntityType = auditEvent.EntityType,
        EntityId = auditEvent.EntityId,
        Action = auditEvent.Action,
        Summary = auditEvent.Summary,
        CreatedAt = auditEvent.CreatedAt,
    };
}
