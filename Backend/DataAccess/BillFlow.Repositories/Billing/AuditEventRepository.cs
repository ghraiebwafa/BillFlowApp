using BillFlow.Database.DbContexts;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Repositories.Billing;

public sealed class AuditEventRepository(BillFlowDbContext db) : IAuditEventRepository
{
    public async Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        auditEvent.CreatedAt = DateTime.UtcNow;
        db.AuditEvents.Add(auditEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetRecentByOwnerAsync(
        Guid ownerId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.AuditEvents
            .Where(e => e.OwnerId == ownerId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
