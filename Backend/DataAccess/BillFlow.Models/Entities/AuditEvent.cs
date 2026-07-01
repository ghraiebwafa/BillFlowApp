using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public Guid ActorUserId { get; set; }

    public string ActorDisplayName { get; set; } = null!;

    public AuditEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string Summary { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
