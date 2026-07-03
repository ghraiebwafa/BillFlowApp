namespace BillFlow.Models.Entities;

public class InvoiceShareToken
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public string Token { get; set; } = null!;

    public bool IsRevoked { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
