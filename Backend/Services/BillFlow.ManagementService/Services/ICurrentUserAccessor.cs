namespace BillFlow.ManagementService.Services;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }

    string? Role { get; }
}
