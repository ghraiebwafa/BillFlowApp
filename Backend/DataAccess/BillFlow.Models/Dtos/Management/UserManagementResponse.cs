using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Management;

public class UserManagementResponse
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
