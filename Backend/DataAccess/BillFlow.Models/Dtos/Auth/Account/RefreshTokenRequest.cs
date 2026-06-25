using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}