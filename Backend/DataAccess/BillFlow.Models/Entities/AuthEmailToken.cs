using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Entities;

public class AuthEmailToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public AuthEmailTokenPurpose Purpose { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive => UsedAt is null && DateTime.UtcNow < ExpiresAt;
}
