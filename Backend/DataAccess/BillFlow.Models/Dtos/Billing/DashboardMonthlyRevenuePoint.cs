namespace BillFlow.Models.Dtos.Billing;

public class DashboardMonthlyRevenuePoint
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Revenue { get; set; }
}
