using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

/// <summary>
/// Production reset uses <see cref="Token"/>.
/// Dev-only direct reset (ALLOW_DEV_RESET_PASSWORD) uses <see cref="Email"/>.
/// </summary>
public class ResetPasswordRequest
{
    [MaxLength(256)]
    public string? Token { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = null!;
}
