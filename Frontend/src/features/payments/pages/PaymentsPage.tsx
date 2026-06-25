import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { InvoiceStatus, type InvoiceSummary } from "../../../domain/billing/invoice";

function formatMoney(amount: number): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(amount);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" }).format(
    new Date(value),
  );
}

export function PaymentsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");

  const { data, isLoading, error } = useQuery({
    queryKey: ["invoices", "payments-view"],
    queryFn: () => managementRequest<InvoiceSummary[]>("/api/v1.0/billing/Invoice/GetAll"),
  });

  const payments = useMemo(() => {
    const rows = (data ?? []).filter(
      (inv) => inv.status === InvoiceStatus.Paid || inv.status === InvoiceStatus.PartiallyPaid,
    );
    const term = search.trim().toLowerCase();
    if (!term) return rows;
    return rows.filter(
      (inv) =>
        inv.invoiceNumber.toLowerCase().includes(term) ||
        inv.clientCompanyName.toLowerCase().includes(term),
    );
  }, [data, search]);

  return (
    <section className="app-screen">
      <PageHeader title={t("nav.payments")} />

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("payments.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </label>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("payments.loadError")}
        </div>
      ) : null}

      <ul className="list-stack">
        {payments.map((row) => (
          <li key={row.id} className="card list-row-static">
            <div className="flex items-start justify-between gap-2">
              <div>
                <p className="font-semibold">{row.invoiceNumber}</p>
                <p className="text-sm text-secondary">{row.clientCompanyName}</p>
                <p className="mt-1 text-xs text-secondary">{formatDate(row.invoiceDate)}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-accent">{formatMoney(row.total)}</p>
                <StatusBadge label={t("payments.completed")} variant="completed" />
              </div>
            </div>
          </li>
        ))}
      </ul>

      {!isLoading && !error && payments.length === 0 ? (
        <div className="card text-center text-secondary">{t("payments.empty")}</div>
      ) : null}
    </section>
  );
}
