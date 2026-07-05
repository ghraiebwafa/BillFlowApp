namespace BillFlow.Models.Dtos.Billing;

public class ShareLinkResponse
{
    public string? Token { get; set; }

    public string? Url { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool AlreadyActive { get; set; }
}
