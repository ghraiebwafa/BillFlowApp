namespace BillFlow.Models.Dtos.Billing;

public class ShareLinkResponse
{
    public string Token { get; set; } = null!;

    public string Url { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }
}
