using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}