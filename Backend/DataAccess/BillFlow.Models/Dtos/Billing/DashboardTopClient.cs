namespace BillFlow.Models.Dtos.Billing;

public class DashboardTopClient
{
    public Guid ClientId { get; set; }

    public string CompanyName { get; set; } = null!;

    public decimal Revenue { get; set; }
}
