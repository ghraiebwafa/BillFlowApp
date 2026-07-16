namespace BillFlow.Models.Dtos.Billing;

public class PortalCheckoutResponse
{
    public bool Configured { get; set; }

    public string? CheckoutUrl { get; set; }

    public string Message { get; set; } = null!;
}
