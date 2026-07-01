using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class AuditEventResponse
{
    public Guid Id { get; set; }

    public Guid ActorUserId { get; set; }

    public string ActorDisplayName { get; set; } = null!;

    public AuditEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string Summary { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
