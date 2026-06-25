using BillFlow.Models.Entities;

namespace BillFlow.Repositories.Interfaces;

public sealed class RefreshTokenRotationResult
{
    public required User User { get; init; }

    public required string OldTokenHash { get; init; }
}
