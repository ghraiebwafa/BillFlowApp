using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Visitor;

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();

    public ICollection<Client> Clients { get; set; } = new List<Client>();

    public ICollection<Item> Items { get; set; } = new List<Item>();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}