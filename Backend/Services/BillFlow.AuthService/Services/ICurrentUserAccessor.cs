namespace BillFlow.AuthService.Services;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
