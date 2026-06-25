import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Download, Share2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { env } from "../../../shared/config/env";
import { useSessionStore } from "../../../shared/auth/session-store";
import {
  InvoiceStatus,
  type InvoiceDetail,
  invoiceStatusLabel,
} from "../../../domain/billing/invoice";
import type { PaymentRecord } from "../../../domain/billing/payment";
import { paymentMethodLabel, PaymentStatus } from "../../../domain/billing/payment";

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

export function InvoiceDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const accessToken = useSessionStore((s) => s.accessToken);

  const { data: invoice, isLoading, error } = useQuery({
    queryKey: ["invoice", id],
    enabled: Boolean(id),
    queryFn: () => managementRequest<InvoiceDetail>(`/api/v1.0/billing/Invoice/GetById/${id}`),
  });

  const { data: payments } = useQuery({
    queryKey: ["invoice-payments", id],
    enabled: Boolean(id),
    queryFn: () =>
      managementRequest<PaymentRecord[]>(`/api/v1.0/billing/Payment/GetByInvoice/${id}`),
  });

  const completedPayments = (payments ?? []).filter((p) => p.status === PaymentStatus.Completed);

  const downloadPdf = async () => {
    if (!id || !accessToken) return;
    const response = await fetch(
      `${env.managementApiUrl}/api/v1.0/billing/Invoice/DownloadPdf/${id}`,
      { headers: { Authorization: `Bearer ${accessToken}` } },
    );
    if (!response.ok) return;
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `${invoice?.invoiceNumber ?? "invoice"}.pdf`;
    anchor.click();
    URL.revokeObjectURL(url);
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
            <button className="btn-secondary flex flex-1 items-center justify-center gap-2" onClick={() => void downloadPdf()} type="button">
              <Download className="h-4 w-4" />
              {t("invoices.downloadPdf")}
            </button>
            <button className="btn-primary flex flex-1 items-center justify-center gap-2" disabled type="button" title={t("common.comingSoon")}>
              <Share2 className="h-4 w-4" />
              {t("invoices.share")}
            </button>
          </div>
        </>
      ) : null}
    </section>
  );
}
