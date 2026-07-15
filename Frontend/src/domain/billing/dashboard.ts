import { z } from "zod";

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

export const dashboardResponseSchema = z.object({
  totalRevenue: z.number(),
  totalInvoices: z.number().int(),
  pendingPaymentsAmount: z.number(),
  overdueInvoicesCount: z.number().int(),
  activeClientsCount: z.number().int(),
  monthlyIncome: z.number(),
  revenueByMonth: z.array(
    z.object({
      year: z.number().int(),
      month: z.number().int(),
      revenue: z.number(),
    }),
  ),
  invoicesByStatus: z.array(
    z.object({
      status: z.number().int(),
      count: z.number().int(),
    }),
  ),
  paymentsByMethod: z.array(
    z.object({
      method: z.number().int(),
      amount: z.number(),
    }),
  ),
  topClients: z.array(
    z.object({
      clientId: z.string().uuid(),
      companyName: z.string(),
      revenue: z.number(),
    }),
  ),
});
