using System.ComponentModel.DataAnnotations;

namespace BillFlow.Models.Dtos.Management;

public class UpdateVisitorRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = null!;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
