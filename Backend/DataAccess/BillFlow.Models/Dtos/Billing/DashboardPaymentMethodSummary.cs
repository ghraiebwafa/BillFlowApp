using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class DashboardPaymentMethodSummary
{
    public PaymentMethod Method { get; set; }

    public decimal Amount { get; set; }
}
