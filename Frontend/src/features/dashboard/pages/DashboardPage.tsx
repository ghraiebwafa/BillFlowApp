import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { LineChart } from "../../../shared/ui/LineChart";
import { managementRequest } from "../../../shared/api/management-client";
import type { DashboardResponse } from "../../../domain/billing/dashboard";
import { ApiError } from "../../../shared/api/api-error";

const PAID_STATUS = 3;
const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

function formatCurrentMonthRange(): string {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1);
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  const fmt = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" });
  return `${fmt.format(start)} – ${fmt.format(end)}`;
}

function countPaidInvoices(data: DashboardResponse): number {
  return data.invoicesByStatus.find((s) => s.status === PAID_STATUS)?.count ?? 0;
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function DashboardPage() {
  const { t } = useTranslation();
  const { data, isLoading, error } = useQuery({
    queryKey: ["dashboard", "summary"],
    queryFn: () =>
      managementRequest<DashboardResponse>("/api/v1.0/billing/Dashboard/GetSummary"),
  });

  const chartPoints =
    data?.revenueByMonth.slice(-6).map((point) => ({
      label: MONTHS[point.month - 1] ?? String(point.month),
      value: point.revenue,
    })) ?? [];

  return (
    <section className="app-screen">
      <PageHeader title={t("nav.dashboard")} subtitle={formatCurrentMonthRange()} />

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}

      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("dashboard.loadError")}
        </div>
      ) : null}

      {data ? (
        <>
          <div className="stat-grid">
            <article className="stat-card">
              <p className="stat-card-label">{t("dashboard.cards.revenue")}</p>
              <p className="stat-card-value">{formatCurrency(data.totalRevenue)}</p>
            </article>
            <article className="stat-card">
              <p className="stat-card-label">{t("dashboard.cards.invoices")}</p>
              <p className="stat-card-value">{data.totalInvoices}</p>
            </article>
            <article className="stat-card">
              <p className="stat-card-label">{t("dashboard.cards.outstanding")}</p>
              <p className="stat-card-value">{formatCurrency(data.pendingPaymentsAmount)}</p>
            </article>
            <article className="stat-card">
              <p className="stat-card-label">{t("dashboard.cards.paid")}</p>
              <p className="stat-card-value">{countPaidInvoices(data)}</p>
            </article>
          </div>

          <LineChart
            title={t("dashboard.chart.title")}
            data={chartPoints}
            emptyLabel={t("dashboard.chart.empty")}
          />
        </>
      ) : null}
    </section>
  );
}
