import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Bell,
  ChevronRight,
  FileText,
  Package,
  PieChart,
  Users,
} from "lucide-react";
import { managementRequest } from "../../../shared/api/management-client";
import type { DashboardResponse } from "../../../domain/billing/dashboard";
import { ApiError } from "../../../shared/api/api-error";

const navCards = [
  { to: "/clients", titleKey: "dashboard.modules.clients.title", descKey: "dashboard.modules.clients.desc", icon: Users },
  { to: "/items", titleKey: "dashboard.modules.items.title", descKey: "dashboard.modules.items.desc", icon: Package },
  { to: "/invoices", titleKey: "dashboard.modules.invoices.title", descKey: "dashboard.modules.invoices.desc", icon: FileText },
  { to: "/reports", titleKey: "dashboard.modules.reminders.title", descKey: "dashboard.modules.reminders.desc", icon: Bell },
  { to: "/reports", titleKey: "dashboard.modules.reports.title", descKey: "dashboard.modules.reports.desc", icon: PieChart },
] as const;

export function DashboardPage() {
  const { t } = useTranslation();
  const { data, isLoading, error } = useQuery({
    queryKey: ["dashboard", "summary"],
    queryFn: () =>
      managementRequest<DashboardResponse>("/api/v1.0/billing/Dashboard/GetSummary"),
  });

  return (
    <section className="mx-auto max-w-3xl space-y-6">
      <div className="auth-hero rounded-2xl px-4 py-6 md:rounded-3xl md:px-6">
        <div className="flex flex-col items-center text-center">
          <img src="/assets/billflow-logo.png" alt="BillFlow" className="mb-2 h-14 w-auto" />
          <p className="text-sm font-medium text-[var(--billflow-maroon)]/85">{t("app.tagline")}</p>
        </div>
      </div>

      <div>
        <h2 className="text-2xl font-semibold">{t("dashboard.title")}</h2>
        <p className="text-sm text-secondary">{t("dashboard.subtitle")}</p>
      </div>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}

      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("dashboard.loadError")}
        </div>
      ) : null}

      {data ? (
        <div className="grid gap-3 sm:grid-cols-3">
          <article className="card text-center">
            <p className="text-xs text-secondary">{t("dashboard.cards.revenue")}</p>
            <p className="text-xl font-semibold">{data.totalRevenue.toFixed(2)}</p>
          </article>
          <article className="card text-center">
            <p className="text-xs text-secondary">{t("dashboard.cards.invoices")}</p>
            <p className="text-xl font-semibold">{data.totalInvoices}</p>
          </article>
          <article className="card text-center">
            <p className="text-xs text-secondary">{t("dashboard.cards.clients")}</p>
            <p className="text-xl font-semibold">{data.activeClientsCount}</p>
          </article>
        </div>
      ) : null}

      <div className="space-y-3">
        {navCards.map(({ to, titleKey, descKey, icon: Icon }) => (
          <Link key={titleKey} to={to} className="dashboard-nav-card">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-[color-mix(in_srgb,var(--billflow-orange)_15%,transparent)] text-accent">
              <Icon className="h-5 w-5" strokeWidth={1.75} />
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-semibold">{t(titleKey)}</p>
              <p className="text-sm text-secondary">{t(descKey)}</p>
            </div>
            <ChevronRight className="h-5 w-5 shrink-0 text-secondary" />
          </Link>
        ))}
      </div>
    </section>
  );
}
