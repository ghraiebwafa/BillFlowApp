using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

public class RegisterRequest
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

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = null!;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }
}