using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Auth.Account;

public class ConfirmEmailRequest
{
    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = null!;
}
