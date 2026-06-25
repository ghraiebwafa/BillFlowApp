namespace BillFlow.Models.Dtos.Billing;

public class ClientResponse
{
    public Guid Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Country { get; set; }

    public string? TaxNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
