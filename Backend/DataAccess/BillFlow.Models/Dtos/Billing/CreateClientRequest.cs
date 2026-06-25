using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Billing;

public class CreateClientRequest
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string ContactName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
