namespace BillFlow.Models.Entities;

public class Client
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Country { get; set; }

    public string? TaxNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
