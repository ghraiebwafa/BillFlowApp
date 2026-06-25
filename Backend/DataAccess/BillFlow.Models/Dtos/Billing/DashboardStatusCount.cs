using BillFlow.Models.Shared.Enums;

namespace BillFlow.Models.Dtos.Billing;

public class DashboardStatusCount
{
    public InvoiceStatus Status { get; set; }

    public int Count { get; set; }
}
