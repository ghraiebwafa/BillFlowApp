using BillFlow.Shared.Constants;

namespace BillFlow.ManagementService.Services.Billing;

public static class BillingAuthorization
{
    public static (Guid? Value, OperationResult<T>? Error) RequireBusinessOwnerId<T>(
        ICurrentUserAccessor currentUser)
    {
        if (!IsBusinessOwner(currentUser))
        {
            return (null, OperationResult<T>.Fail(
                "Business owner role is required.",
                StatusCodes.Status403Forbidden));
        }

        if (currentUser.UserId is null)
        {
            return (null, OperationResult<T>.Fail(
                "Authentication required.",
                StatusCodes.Status401Unauthorized));
        }

        return (currentUser.UserId, null);
    }

    public static bool IsBusinessOwner(ICurrentUserAccessor currentUser) =>
        string.Equals(currentUser.Role, RoleNames.Visitor, StringComparison.OrdinalIgnoreCase);
}
