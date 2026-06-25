namespace BillFlow.Models.Dtos.Billing;

public class DashboardResponse
{
    public decimal TotalRevenue { get; set; }

    public int TotalInvoices { get; set; }

    public decimal PendingPaymentsAmount { get; set; }

    public int OverdueInvoicesCount { get; set; }

    public int ActiveClientsCount { get; set; }

    public decimal MonthlyIncome { get; set; }

    public IReadOnlyList<DashboardMonthlyRevenuePoint> RevenueByMonth { get; set; } = [];

    public IReadOnlyList<DashboardStatusCount> InvoicesByStatus { get; set; } = [];

    public IReadOnlyList<DashboardPaymentMethodSummary> PaymentsByMethod { get; set; } = [];

    public IReadOnlyList<DashboardTopClient> TopClients { get; set; } = [];
}
