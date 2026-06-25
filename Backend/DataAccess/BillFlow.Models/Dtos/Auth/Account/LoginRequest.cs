using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = null!;
}