using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

public class ChangePasswordRequest
{
    [Required]
    [MaxLength(128)]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = null!;
}