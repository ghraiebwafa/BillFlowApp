import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Filter, Plus, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { PaginationBar } from "../../../shared/ui/PaginationBar";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { buildPageQuery, pagedSchema, type PagedResponse } from "../../../domain/billing/paging";
import { invoiceSummarySchema } from "../../../domain/billing/schemas";
import {
  InvoiceStatus,
  type InvoiceSummary,
  invoiceStatusLabel,
} from "../../../domain/billing/invoice";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";

type FilterKey = "all" | "paid" | "unpaid" | "partial";

const PAGE_SIZE = 50;
const pagedInvoiceSchema = pagedSchema(invoiceSummarySchema);

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" }).format(
    new Date(value),
  );
}

function statusVariant(status: InvoiceStatus): "paid" | "partial" | "unpaid" | "draft" {
  if (status === InvoiceStatus.Paid) return "paid";
  if (status === InvoiceStatus.PartiallyPaid) return "partial";
  if (status === InvoiceStatus.Draft || status === InvoiceStatus.Cancelled) return "draft";
  return "unpaid";
}

function filterToQuery(filter: FilterKey): {
  status?: InvoiceStatus;
  statuses?: InvoiceStatus[];
} {
  if (filter === "paid") return { status: InvoiceStatus.Paid };
  if (filter === "partial") return { status: InvoiceStatus.PartiallyPaid };
  if (filter === "unpaid") {
    return { statuses: [InvoiceStatus.Sent, InvoiceStatus.Overdue] };
  }
  return {};
}

export function InvoicesPage() {
  const { t } = useTranslation();
  const currency = useCompanyCurrency();
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, filter]);

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ["invoices", { search: debouncedSearch, filter, page, pageSize: PAGE_SIZE }],
    queryFn: () => {
      const filterQuery = filterToQuery(filter);
      return managementRequest<PagedResponse<InvoiceSummary>>(
        `${billingApi.invoices}${buildPageQuery({
          search: debouncedSearch,
          status: filterQuery.status,
          statuses: filterQuery.statuses,
          page,
          pageSize: PAGE_SIZE,
        })}`,
        { schema: pagedInvoiceSchema },
      );
    },
    placeholderData: (previous) => previous,
  });

  const invoices = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

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
          aria-label={t("invoices.searchPlaceholder")}
        />
        <Filter className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
      </label>

      <div className="filter-chips" role="group" aria-label={t("nav.invoices")}>
        {filters.map(({ key, labelKey }) => (
          <button
            key={key}
            className={filter === key ? "filter-chip active" : "filter-chip"}
            onClick={() => setFilter(key)}
            type="button"
            aria-pressed={filter === key}
          >
            {t(labelKey)}
          </button>
        ))}
      </div>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500" role="alert">
          {error instanceof ApiError ? error.message : t("invoices.loadError")}
        </div>
      ) : null}

      {!isLoading && !error && invoices.length > 0 ? (
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
                    <span className="font-semibold text-primary">
                      {formatMoney(invoice.total, currency)}
                    </span>
                  </div>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      ) : null}

      {!isLoading && !error && invoices.length === 0 ? (
        <div className="card text-center text-secondary">{t("invoices.empty")}</div>
      ) : null}

      <PaginationBar
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
        disabled={isFetching}
      />

      <Link className="fab" to="/invoices/new" aria-label={t("invoices.create")}>
        <Plus className="h-6 w-6" />
      </Link>
    </section>
  );
}
