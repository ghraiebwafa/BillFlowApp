import { useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { CreditCard, Download, FileText } from "lucide-react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { portalApi } from "../../../domain/billing/api-paths";
import { env } from "../../../shared/config/env";
import { formatMoney } from "../../../shared/lib/money";
import { InvoiceStatus, invoiceStatusLabel } from "../../../domain/billing/invoice";
import { toast } from "../../../shared/ui/toast-store";

const publicLineItemSchema = z.object({
  id: z.string().uuid(),
  description: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  lineTotal: z.number(),
});

const publicIssuerSchema = z.object({
  companyName: z.string(),
  address: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  phoneNumber: z.string().nullable().optional(),
  email: z.string().nullable().optional(),
  taxNumber: z.string().nullable().optional(),
  currency: z.string(),
  brandColor: z.union([z.literal(""), z.string().regex(/^#[0-9A-Fa-f]{6}$/)]).nullable().optional(),
  invoiceFooterNote: z.string().nullable().optional(),
});

const publicInvoiceSchema = z.object({
  invoiceNumber: z.string(),
  status: z.number().int(),
  clientCompanyName: z.string(),
  clientContactName: z.string(),
  invoiceDate: z.string(),
  dueDate: z.string(),
  subtotal: z.number(),
  taxRate: z.number(),
  taxAmount: z.number(),
  total: z.number(),
  notes: z.string().nullable().optional(),
  lineItems: z.array(publicLineItemSchema),
  issuer: publicIssuerSchema.nullable().optional(),
});

type PublicInvoice = z.infer<typeof publicInvoiceSchema>;

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

function sanitizeBrandColor(value?: string | null): string {
  if (!value || !/^#[0-9A-Fa-f]{6}$/.test(value)) return "#ff6b00";
  return value;
}

async function fetchPublicInvoice(token: string): Promise<PublicInvoice> {
  const url = `${env.managementApiUrl}${portalApi.invoice(token)}`;
  const response = await fetch(url, { headers: { Accept: "application/json" } });

  if (!response.ok) {
    throw new Error("portal_unavailable");
  }

  return publicInvoiceSchema.parse(await response.json());
}

export function PortalPage() {
  const { t } = useTranslation();
  const { token } = useParams<{ token: string }>();

  const { data: invoice, isLoading, error } = useQuery({
    queryKey: ["portal", token],
    enabled: Boolean(token),
    queryFn: () => fetchPublicInvoice(token!),
    retry: false,
    staleTime: 5 * 60_000,
  });

  const brandColor = sanitizeBrandColor(invoice?.issuer?.brandColor);
  const currency = invoice?.issuer?.currency ?? "USD";

  const downloadPdf = async () => {
    if (!token) return;
    const url = `${env.managementApiUrl}${portalApi.invoicePdf(token)}`;
    const res = await fetch(url);
    if (!res.ok) return;
    const blob = await res.blob();
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = `${invoice?.invoiceNumber ?? "invoice"}.pdf`;
    a.click();
    URL.revokeObjectURL(a.href);
  };

  const checkoutMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${env.managementApiUrl}${portalApi.checkout(token!)}`, {
        method: "POST",
        headers: { Accept: "application/json" },
      });
      if (!res.ok) throw new Error("checkout_failed");
      return z
        .object({
          configured: z.boolean(),
          checkoutUrl: z.string().nullable().optional(),
          message: z.string(),
        })
        .parse(await res.json());
    },
    onSuccess: (result) => {
      if (result.configured && result.checkoutUrl) {
        window.location.href = result.checkoutUrl;
        return;
      }
      toast(result.message, "info");
    },
    onError: () => {
      toast(t("portal.checkoutError"), "error");
    },
  });

  const canPayOnline =
    invoice
    && (invoice.status === InvoiceStatus.Sent
      || invoice.status === InvoiceStatus.Overdue
      || invoice.status === InvoiceStatus.PartiallyPaid);

  if (!token) {
    return (
      <div className="portal-wrapper">
        <div className="portal-card">
          <p className="text-secondary">{t("portal.notFound")}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="portal-wrapper">
      <div className="portal-card">
        {/* Brand header bar */}
        <div className="portal-brand-bar" style={{ background: brandColor }} />

        {isLoading ? (
          <div className="portal-body">
            <p className="text-secondary">{t("app.loading")}</p>
          </div>
        ) : null}

        {error ? (
          <div className="portal-body">
            <div className="portal-error-icon">
              <FileText className="h-12 w-12 text-secondary" />
            </div>
            <h2 className="portal-error-title">{t("portal.expired")}</h2>
            <p className="text-secondary text-sm">{t("portal.notFound")}</p>
          </div>
        ) : null}

        {invoice ? (
          <div className="portal-body">
            {/* Header */}
            <div className="portal-header">
              <div>
                <h1 className="portal-invoice-number">{invoice.invoiceNumber}</h1>
                {invoice.issuer ? (
                  <p className="text-sm text-secondary portal-issuer-name">{invoice.issuer.companyName}</p>
                ) : null}
              </div>
              <StatusBadge label={invoiceStatusLabel(invoice.status as InvoiceStatus, t)} variant={statusVariant(invoice.status as InvoiceStatus)} />
            </div>

            {/* Issuer + Client info */}
            <div className="portal-parties">
              {invoice.issuer ? (
                <div className="portal-party">
                  <p className="portal-party-label">{t("portal.from")}</p>
                  <p className="font-semibold">{invoice.issuer.companyName}</p>
                  {invoice.issuer.address ? <p className="text-sm text-secondary">{invoice.issuer.address}</p> : null}
                  {invoice.issuer.country ? <p className="text-sm text-secondary">{invoice.issuer.country}</p> : null}
                  {invoice.issuer.email ? <p className="text-sm text-secondary">{invoice.issuer.email}</p> : null}
                  {invoice.issuer.phoneNumber ? <p className="text-sm text-secondary">{invoice.issuer.phoneNumber}</p> : null}
                  {invoice.issuer.taxNumber ? <p className="text-xs text-secondary">{t("portal.taxId")}: {invoice.issuer.taxNumber}</p> : null}
                </div>
              ) : null}
              <div className="portal-party">
                <p className="portal-party-label">{t("portal.billTo")}</p>
                <p className="font-semibold">{invoice.clientCompanyName}</p>
                <p className="text-sm text-secondary">{invoice.clientContactName}</p>
              </div>
            </div>

            {/* Dates */}
            <div className="portal-dates">
              <div>
                <p className="text-xs text-secondary">{t("invoices.issueDate")}</p>
                <p className="font-medium">{formatDate(invoice.invoiceDate)}</p>
              </div>
              <div>
                <p className="text-xs text-secondary">{t("invoices.dueDate")}</p>
                <p className="font-medium">{formatDate(invoice.dueDate)}</p>
              </div>
            </div>

            {/* Line items table */}
            <div className="portal-items">
              <table className="portal-table">
                <thead>
                  <tr>
                    <th className="text-left">{t("invoices.itemDescription")}</th>
                    <th className="text-right">{t("invoices.quantity")}</th>
                    <th className="text-right">{t("invoices.unitPrice")}</th>
                    <th className="text-right">{t("invoices.total")}</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.lineItems.map((item) => (
                    <tr key={item.id}>
                      <td>{item.description}</td>
                      <td className="text-right">{item.quantity}</td>
                      <td className="text-right">{formatMoney(item.unitPrice, currency)}</td>
                      <td className="text-right font-medium">{formatMoney(item.lineTotal, currency)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Totals */}
            <div className="portal-totals">
              <div className="portal-total-row">
                <span>{t("invoices.subtotal")}</span>
                <span>{formatMoney(invoice.subtotal, currency)}</span>
              </div>
              {invoice.taxRate > 0 ? (
                <div className="portal-total-row">
                  <span>{t("invoices.tax", { rate: invoice.taxRate })}</span>
                  <span>{formatMoney(invoice.taxAmount, currency)}</span>
                </div>
              ) : null}
              <div className="portal-total-row portal-total-row--grand" style={{ color: brandColor }}>
                <span>{t("invoices.total")}</span>
                <span>{formatMoney(invoice.total, currency)}</span>
              </div>
            </div>

            {/* Notes */}
            {invoice.notes ? (
              <div className="portal-notes">
                <p className="text-xs text-secondary font-semibold">{t("portal.notes")}</p>
                <p className="text-sm">{invoice.notes}</p>
              </div>
            ) : null}

            {/* Download button */}
            <div className="portal-actions">
              <button
                className="btn-primary flex items-center justify-center gap-2"
                style={{ background: brandColor }}
                onClick={() => void downloadPdf()}
                type="button"
              >
                <Download className="h-4 w-4" />
                {t("invoices.downloadPdf")}
              </button>
              {canPayOnline ? (
                <button
                  className="btn-secondary flex items-center justify-center gap-2"
                  disabled={checkoutMutation.isPending}
                  onClick={() => void checkoutMutation.mutate()}
                  type="button"
                >
                  <CreditCard className="h-4 w-4" />
                  {checkoutMutation.isPending ? t("app.loading") : t("portal.payOnline")}
                </button>
              ) : null}
            </div>

            {/* Footer */}
            {invoice.issuer?.invoiceFooterNote ? (
              <p className="portal-footer-note">{invoice.issuer.invoiceFooterNote}</p>
            ) : null}

            <p className="portal-powered">
              Powered by <span className="font-semibold" style={{ color: brandColor }}>BillFlow</span>
            </p>
          </div>
        ) : null}
      </div>
    </div>
  );
}
