import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { buildPageQuery, type PagedResponse } from "../../../domain/billing/paging";
import { paymentRecordSchema } from "../../../domain/billing/schemas";
import type { PaymentRecord } from "../../../domain/billing/payment";
import { paymentMethodLabel } from "../../../domain/billing/payment";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";

function useDebouncedValue(value: string, delayMs = 300): string {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" }).format(
    new Date(value),
  );
}

const pagedPaymentSchema = z.object({
  items: z.array(paymentRecordSchema),
  totalCount: z.number().int(),
  page: z.number().int(),
  pageSize: z.number().int(),
});

export function PaymentsPage() {
  const { t } = useTranslation();
  const currency = useCompanyCurrency();
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const { data, isLoading, error } = useQuery({
    queryKey: ["payments", debouncedSearch],
    queryFn: () =>
      managementRequest<PagedResponse<PaymentRecord>>(
        `${billingApi.payments}${buildPageQuery({
          search: debouncedSearch,
          page: 1,
          pageSize: 50,
        })}`,
        { schema: pagedPaymentSchema },
      ),
  });

  const payments = data?.items ?? [];

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
                <p className="text-sm text-secondary">{paymentMethodLabel(row.method)}</p>
                <p className="mt-1 text-xs text-secondary">{formatDate(row.paymentDate)}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-accent">{formatMoney(row.amount, currency)}</p>
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
