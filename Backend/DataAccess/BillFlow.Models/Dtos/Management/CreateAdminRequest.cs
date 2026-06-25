using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Management;

public class CreateAdminRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = null!;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }
}
