import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { PaginationBar } from "../../../shared/ui/PaginationBar";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { buildPageQuery, pagedSchema, type PagedResponse } from "../../../domain/billing/paging";
import { paymentRecordSchema } from "../../../domain/billing/schemas";
import {
  PaymentStatus,
  type PaymentRecord,
  paymentMethodLabel,
  paymentStatusLabel,
} from "../../../domain/billing/payment";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";
import { toast } from "../../../shared/ui/toast-store";

const PAGE_SIZE = 50;
const pagedPaymentSchema = pagedSchema(paymentRecordSchema);

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" }).format(
    new Date(value),
  );
}

function paymentBadgeVariant(status: PaymentStatus): "paid" | "partial" | "draft" | "completed" {
  if (status === PaymentStatus.Completed) return "completed";
  if (status === PaymentStatus.Refunded) return "partial";
  return "draft";
}

export function PaymentsPage() {
  const { t } = useTranslation();
  const currency = useCompanyCurrency();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ["payments", { search: debouncedSearch, page, pageSize: PAGE_SIZE }],
    queryFn: () =>
      managementRequest<PagedResponse<PaymentRecord>>(
        `${billingApi.payments}${buildPageQuery({
          search: debouncedSearch,
          page,
          pageSize: PAGE_SIZE,
        })}`,
        { schema: pagedPaymentSchema },
      ),
    placeholderData: (previous) => previous,
  });

  const refundMutation = useMutation({
    mutationFn: (paymentId: string) =>
      managementRequest<PaymentRecord>(billingApi.paymentRefund(paymentId), {
        method: "POST",
        schema: paymentRecordSchema,
      }),
    onSuccess: (payment) => {
      void queryClient.invalidateQueries({ queryKey: ["payments"] });
      void queryClient.invalidateQueries({ queryKey: ["invoice", payment.invoiceId] });
      void queryClient.invalidateQueries({ queryKey: ["invoice-payments", payment.invoiceId] });
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("toast.paymentRefunded"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("common.actionFailed"), "error");
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (paymentId: string) =>
      managementRequest<PaymentRecord>(billingApi.paymentCancel(paymentId), {
        method: "POST",
        schema: paymentRecordSchema,
      }),
    onSuccess: (payment) => {
      void queryClient.invalidateQueries({ queryKey: ["payments"] });
      void queryClient.invalidateQueries({ queryKey: ["invoice", payment.invoiceId] });
      void queryClient.invalidateQueries({ queryKey: ["invoice-payments", payment.invoiceId] });
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("toast.paymentCancelled"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("common.actionFailed"), "error");
    },
  });

  const payments = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const actionPending = refundMutation.isPending || cancelMutation.isPending;

  return (
    <section className="app-screen">
      <PageHeader title={t("nav.payments")} />

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("payments.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label={t("payments.searchPlaceholder")}
        />
      </label>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500" role="alert">
          {error instanceof ApiError ? error.message : t("payments.loadError")}
        </div>
      ) : null}

      {!isLoading && !error && payments.length > 0 ? (
        <ul className="list-stack">
          {payments.map((row) => (
            <li key={row.id} className="card list-row-static">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <p className="font-semibold">
                    <Link className="text-accent no-underline" to={`/invoices/${row.invoiceId}`}>
                      {row.invoiceNumber}
                    </Link>
                  </p>
                  <p className="text-sm text-secondary">{paymentMethodLabel(row.method, t)}</p>
                  <p className="mt-1 text-xs text-secondary">{formatDate(row.paymentDate)}</p>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-accent">{formatMoney(row.amount, currency)}</p>
                  <StatusBadge
                    label={paymentStatusLabel(row.status, t)}
                    variant={paymentBadgeVariant(row.status)}
                  />
                </div>
              </div>
              {row.status === PaymentStatus.Completed ? (
                <div className="mt-2 flex flex-wrap gap-2">
                  <button
                    className="btn-ghost text-sm"
                    disabled={actionPending}
                    onClick={() => {
                      if (window.confirm(t("payments.refundConfirm"))) {
                        refundMutation.mutate(row.id);
                      }
                    }}
                    type="button"
                  >
                    {t("payments.refund")}
                  </button>
                  <button
                    className="btn-ghost text-sm text-red-500"
                    disabled={actionPending}
                    onClick={() => {
                      if (window.confirm(t("payments.cancelConfirm"))) {
                        cancelMutation.mutate(row.id);
                      }
                    }}
                    type="button"
                  >
                    {t("payments.cancelPayment")}
                  </button>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      ) : null}

      {!isLoading && !error && payments.length === 0 ? (
        <div className="card text-center text-secondary">{t("payments.empty")}</div>
      ) : null}

      <PaginationBar
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
        disabled={isFetching}
      />
    </section>
  );
}
