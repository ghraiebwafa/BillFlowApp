import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, Link2, Mail, Send, Unlink } from "lucide-react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { managementRequest } from "../../../shared/api/management-client";
import { downloadWithAuth } from "../../../shared/api/download-with-auth";
import { ApiError } from "../../../shared/api/api-error";
import {
  InvoiceStatus,
  type InvoiceDetail,
  invoiceStatusLabel,
} from "../../../domain/billing/invoice";
import type { PaymentRecord } from "../../../domain/billing/payment";
import { paymentMethodLabel, PaymentStatus } from "../../../domain/billing/payment";
import { invoiceDetailSchema, paymentRecordSchema } from "../../../domain/billing/schemas";
import { toast } from "../../../shared/ui/toast-store";

const shareLinkSchema = z.object({
  token: z.string(),
  url: z.string(),
  expiresAt: z.string().nullable().optional(),
});

function formatMoney(amount: number): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(amount);
}

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

function canEmailInvoice(status: InvoiceStatus): boolean {
  return (
    status === InvoiceStatus.Sent
    || status === InvoiceStatus.Overdue
    || status === InvoiceStatus.PartiallyPaid
    || status === InvoiceStatus.Paid
  );
}

export function InvoiceDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();

  const { data: invoice, isLoading, error } = useQuery({
    queryKey: ["invoice", id],
    enabled: Boolean(id),
    queryFn: () =>
      managementRequest<InvoiceDetail>(`/api/v1.0/billing/Invoice/GetById/${id}`, {
        schema: invoiceDetailSchema,
      }),
  });

  const { data: payments } = useQuery({
    queryKey: ["invoice-payments", id],
    enabled: Boolean(id),
    queryFn: () =>
      managementRequest<PaymentRecord[]>(`/api/v1.0/billing/Payment/GetByInvoice/${id}`, {
        schema: z.array(paymentRecordSchema),
      }),
  });

  const sendMutation = useMutation({
    mutationFn: () =>
      managementRequest<InvoiceDetail>(`/api/v1.0/billing/Invoice/Send/${id}`, {
        method: "POST",
        schema: invoiceDetailSchema,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(["invoice", id], updated);
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("toast.invoiceSent"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("invoices.actionError"), "error");
    },
  });

  const emailMutation = useMutation({
    mutationFn: () =>
      managementRequest<{ message: string }>(`/api/v1.0/billing/Invoice/Email/${id}`, {
        method: "POST",
        schema: z.object({ message: z.string() }),
      }),
    onSuccess: (response) => {
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      const skipped = response.message.toLowerCase().includes("smtp");
      toast(
        skipped ? t("invoices.emailSkipped") : t("toast.invoiceEmailed"),
        skipped ? "info" : "success",
      );
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("invoices.actionError"), "error");
    },
  });

  const shareMutation = useMutation({
    mutationFn: () =>
      managementRequest<{ token: string; url: string }>(`/api/v1.0/billing/Invoice/ShareLink/${id}`, {
        method: "POST",
        schema: shareLinkSchema,
      }),
    onSuccess: (data) => {
      const portalUrl = `${window.location.origin}/portal/${data.token}`;
      void navigator.clipboard.writeText(portalUrl);
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("invoices.shareLinkCopied"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("invoices.actionError"), "error");
    },
  });

  const revokeMutation = useMutation({
    mutationFn: () =>
      managementRequest<{ message: string }>(`/api/v1.0/billing/Invoice/ShareLink/${id}`, {
        method: "DELETE",
        schema: z.object({ message: z.string() }),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("invoices.shareLinkRevoked"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("invoices.actionError"), "error");
    },
  });

  const completedPayments = (payments ?? []).filter((p) => p.status === PaymentStatus.Completed);

  const downloadPdf = async () => {
    if (!id) return;
    try {
      await downloadWithAuth(
        `/api/v1.0/billing/Invoice/DownloadPdf/${id}`,
        `${invoice?.invoiceNumber ?? "invoice"}.pdf`,
      );
    } catch {
      toast(t("invoices.actionError"), "error");
    }
  };

  if (!id) return null;

  return (
    <section className="app-screen">
      <PageHeader title={invoice?.invoiceNumber ?? t("invoices.detail")} backTo="/invoices" />

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("invoices.loadError")}
        </div>
      ) : null}

      {invoice ? (
        <>
          <div className="flex justify-center">
            <StatusBadge label={invoiceStatusLabel(invoice.status)} variant={statusVariant(invoice.status)} />
          </div>

          <div className="detail-section card">
            <h3 className="detail-section-title">{t("invoices.billTo")}</h3>
            <p className="font-semibold">{invoice.clientCompanyName}</p>
            <p className="text-sm text-secondary">{invoice.clientContactName}</p>
            <p className="text-sm text-secondary">{invoice.clientEmail}</p>
          </div>

          <div className="detail-grid">
            <div className="card">
              <p className="text-xs text-secondary">{t("invoices.issueDate")}</p>
              <p className="font-medium">{formatDate(invoice.invoiceDate)}</p>
            </div>
            <div className="card">
              <p className="text-xs text-secondary">{t("invoices.dueDate")}</p>
              <p className="font-medium">{formatDate(invoice.dueDate)}</p>
            </div>
          </div>

          <div className="card invoice-total-card">
            <div className="invoice-total-row">
              <span>{t("invoices.subtotal")}</span>
              <span>{formatMoney(invoice.subtotal)}</span>
            </div>
            <div className="invoice-total-row">
              <span>{t("invoices.tax", { rate: invoice.taxRate })}</span>
              <span>{formatMoney(invoice.taxAmount)}</span>
            </div>
            <div className="invoice-total-row invoice-total-row--grand">
              <span>{t("invoices.total")}</span>
              <span>{formatMoney(invoice.total)}</span>
            </div>
          </div>

          <div className="detail-section">
            <h3 className="detail-section-title">{t("invoices.lineItems")}</h3>
            <ul className="list-stack">
              {invoice.lineItems.map((item) => (
                <li key={item.id} className="card list-row-static">
                  <p className="font-medium">{item.description}</p>
                  <p className="text-sm text-secondary">
                    {item.quantity} × {formatMoney(item.unitPrice)}
                  </p>
                  <p className="text-right font-semibold text-accent">{formatMoney(item.lineTotal)}</p>
                </li>
              ))}
            </ul>
          </div>

          {completedPayments.length > 0 ? (
            <div className="detail-section">
              <h3 className="detail-section-title">{t("invoices.paymentHistory")}</h3>
              <ul className="list-stack">
                {completedPayments.map((payment) => (
                  <li key={payment.id} className="card list-row-static">
                    <div className="flex justify-between gap-2">
                      <span className="font-medium">{formatMoney(payment.amount)}</span>
                      <StatusBadge label={t("payments.completed")} variant="completed" />
                    </div>
                    <p className="text-sm text-secondary">
                      {paymentMethodLabel(payment.method)} · {formatDate(payment.paymentDate)}
                    </p>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          <div className="detail-actions">
            {invoice.status === InvoiceStatus.Draft ? (
              <button
                className="btn-primary flex flex-1 items-center justify-center gap-2"
                disabled={sendMutation.isPending}
                onClick={() => void sendMutation.mutate()}
                type="button"
              >
                <Send className="h-4 w-4" />
                {sendMutation.isPending ? t("app.loading") : t("invoices.send")}
              </button>
            ) : null}

            {canEmailInvoice(invoice.status) ? (
              <button
                className="btn-primary flex flex-1 items-center justify-center gap-2"
                disabled={emailMutation.isPending}
                onClick={() => void emailMutation.mutate()}
                type="button"
              >
                <Mail className="h-4 w-4" />
                {emailMutation.isPending ? t("app.loading") : t("invoices.email")}
              </button>
            ) : null}

            {invoice.status !== InvoiceStatus.Draft ? (
              <button
                className="btn-secondary flex flex-1 items-center justify-center gap-2"
                onClick={() => void downloadPdf()}
                type="button"
              >
                <Download className="h-4 w-4" />
                {t("invoices.downloadPdf")}
              </button>
            ) : null}

            {invoice.status !== InvoiceStatus.Draft && invoice.status !== InvoiceStatus.Cancelled ? (
              <>
                <button
                  className="btn-secondary flex flex-1 items-center justify-center gap-2"
                  disabled={shareMutation.isPending}
                  onClick={() => void shareMutation.mutate()}
                  type="button"
                >
                  <Link2 className="h-4 w-4" />
                  {shareMutation.isPending ? t("app.loading") : t("invoices.shareLink")}
                </button>
                <button
                  className="btn-ghost flex flex-1 items-center justify-center gap-2 text-sm"
                  disabled={revokeMutation.isPending}
                  onClick={() => void revokeMutation.mutate()}
                  type="button"
                >
                  <Unlink className="h-4 w-4" />
                  {revokeMutation.isPending ? t("app.loading") : t("invoices.revokeLink")}
                </button>
              </>
            ) : null}
          </div>

          {invoice.status === InvoiceStatus.Draft ? (
            <p className="text-center text-sm text-secondary">
              <Link to="/settings/company" className="text-accent no-underline">
                {t("settings.title")}
              </Link>
            </p>
          ) : null}
        </>
      ) : null}
    </section>
  );
}
