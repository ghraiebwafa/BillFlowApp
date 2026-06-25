import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Filter, Plus, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import {
  InvoiceStatus,
  type InvoiceSummary,
  invoiceStatusClass,
  invoiceStatusLabel,
} from "../../../domain/billing/invoice";

type FilterKey = "all" | "paid" | "unpaid" | "partial";

function useDebouncedValue(value: string, delayMs = 300): string {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}

function formatMoney(amount: number): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(amount);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" }).format(
    new Date(value),
  );
}

function matchesFilter(status: InvoiceStatus, filter: FilterKey): boolean {
  if (filter === "all") return true;
  if (filter === "paid") return status === InvoiceStatus.Paid;
  if (filter === "partial") return status === InvoiceStatus.PartiallyPaid;
  return status === InvoiceStatus.Sent || status === InvoiceStatus.Overdue;
}

function statusVariant(status: InvoiceStatus): "paid" | "partial" | "unpaid" | "draft" {
  if (status === InvoiceStatus.Paid) return "paid";
  if (status === InvoiceStatus.PartiallyPaid) return "partial";
  if (status === InvoiceStatus.Draft || status === InvoiceStatus.Cancelled) return "draft";
  return "unpaid";
}

export function InvoicesPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");
  const debouncedSearch = useDebouncedValue(search);

  const { data, isLoading, error } = useQuery({
    queryKey: ["invoices", debouncedSearch],
    queryFn: () => {
      const params = debouncedSearch.trim()
        ? `?search=${encodeURIComponent(debouncedSearch.trim())}`
        : "";
      return managementRequest<InvoiceSummary[]>(`/api/v1.0/billing/Invoice/GetAll${params}`);
    },
  });

  const invoices = useMemo(
    () => (data ?? []).filter((inv) => matchesFilter(inv.status, filter)),
    [data, filter],
  );

  const filters: { key: FilterKey; labelKey: string }[] = [
    { key: "all", labelKey: "invoices.filters.all" },
    { key: "paid", labelKey: "invoices.filters.paid" },
    { key: "unpaid", labelKey: "invoices.filters.unpaid" },
    { key: "partial", labelKey: "invoices.filters.partial" },
  ];

  return (
    <section className="app-screen">
      <PageHeader title={t("nav.invoices")} />

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("invoices.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Filter className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
      </label>

      <div className="filter-chips">
        {filters.map(({ key, labelKey }) => (
          <button
            key={key}
            className={filter === key ? "filter-chip active" : "filter-chip"}
            onClick={() => setFilter(key)}
            type="button"
          >
            {t(labelKey)}
          </button>
        ))}
      </div>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("invoices.loadError")}
        </div>
      ) : null}

      <ul className="list-stack">
        {invoices.map((invoice) => (
          <li key={invoice.id}>
            <Link className="list-row" to={`/invoices/${invoice.id}`}>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate font-semibold">{invoice.invoiceNumber}</p>
                  <StatusBadge
                    label={invoiceStatusLabel(invoice.status)}
                    variant={statusVariant(invoice.status)}
                  />
                </div>
                <p className="truncate text-sm text-secondary">{invoice.clientCompanyName}</p>
                <div className="mt-1 flex items-center justify-between text-xs text-secondary">
                  <span>{formatDate(invoice.invoiceDate)}</span>
                  <span className="font-semibold text-primary">{formatMoney(invoice.total)}</span>
                </div>
              </div>
            </Link>
          </li>
        ))}
      </ul>

      {!isLoading && !error && invoices.length === 0 ? (
        <div className="card text-center text-secondary">{t("invoices.empty")}</div>
      ) : null}

      <Link className="fab" to="/invoices/new" aria-label={t("invoices.create")}>
        <Plus className="h-6 w-6" />
      </Link>
    </section>
  );
}
