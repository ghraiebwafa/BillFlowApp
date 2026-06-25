export type DashboardResponse = {
  totalRevenue: number;
  totalInvoices: number;
  pendingPaymentsAmount: number;
  overdueInvoicesCount: number;
  activeClientsCount: number;
  monthlyIncome: number;
  revenueByMonth: Array<{ year: number; month: number; revenue: number }>;
  invoicesByStatus: Array<{ status: number; count: number }>;
  paymentsByMethod: Array<{ method: number; amount: number }>;
  topClients: Array<{ clientId: string; companyName: string; revenue: number }>;
};
