namespace BillFlow.Shared.Constants;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Visitor = "Visitor";

    /// <summary>Business owner role (registered via Auth API). Same as <see cref="Visitor"/>.</summary>
    public const string BusinessOwner = Visitor;

    public const string AdminOrSuperAdmin = "Admin,SuperAdmin";
}
