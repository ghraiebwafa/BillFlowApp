import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Banknote,
  CheckCircle2,
  Copy,
  Download,
  Link2,
  Mail,
  Pencil,
  Send,
  Trash2,
  Unlink,
  XCircle,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { StatusBadge } from "../../../shared/ui/StatusBadge";
import { FormField } from "../../../shared/ui/FormField";
import { FormTextArea } from "../../../shared/ui/FormTextArea";
import { managementRequest } from "../../../shared/api/management-client";
import { downloadWithAuth } from "../../../shared/api/download-with-auth";
import { ApiError } from "../../../shared/api/api-error";
import {
  InvoiceStatus,
  canCancelInvoice,
  canDeleteInvoice,
  canMarkInvoicePaid,
  canReceivePayment,
  type InvoiceDetail,
  invoiceStatusLabel,
} from "../../../domain/billing/invoice";
import {
  PaymentMethod,
  PaymentStatus,
  type PaymentRecord,
  paymentMethodLabel,
  paymentStatusLabel,
} from "../../../domain/billing/payment";
import { invoiceDetailSchema, paymentRecordSchema } from "../../../domain/billing/schemas";
import { billingApi } from "../../../domain/billing/api-paths";
import { toast } from "../../../shared/ui/toast-store";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";

const shareLinkSchema = z.object({
  token: z.string().optional(),
  url: z.string().optional(),
  expiresAt: z.string().nullable().optional(),
  alreadyActive: z.boolean().optional(),
});

const messageSchema = z.object({ message: z.string() });

const PAYMENT_METHODS = [
  PaymentMethod.Cash,
  PaymentMethod.BankTransfer,
  PaymentMethod.CreditCard,
  PaymentMethod.PayPal,
  PaymentMethod.Stripe,
] as const;

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric", year: "numeric" }).format(
    new Date(value),
  );
}

function todayInputValue(): string {
  return new Date().toISOString().slice(0, 10);
}

function statusVariant(status: InvoiceStatus): "paid" | "partial" | "unpaid" | "draft" {
  if (status === InvoiceStatus.Paid) return "paid";
  if (status === InvoiceStatus.PartiallyPaid) return "partial";
  if (status === InvoiceStatus.Draft || status === InvoiceStatus.Cancelled) return "draft";
  return "unpaid";
}

function paymentBadgeVariant(status: PaymentStatus): "paid" | "partial" | "draft" | "completed" {
  if (status === PaymentStatus.Completed) return "completed";
  if (status === PaymentStatus.Refunded) return "partial";
  return "draft";
}

function canEmailInvoice(status: InvoiceStatus): boolean {
  return (
    status === InvoiceStatus.Sent
    || status === InvoiceStatus.Overdue
    || status === InvoiceStatus.PartiallyPaid
    || status === InvoiceStatus.Paid
  );
}

function invalidateBillingCaches(queryClient: ReturnType<typeof useQueryClient>, invoiceId: string) {
  void queryClient.invalidateQueries({ queryKey: ["invoice", invoiceId] });
  void queryClient.invalidateQueries({ queryKey: ["invoice-payments", invoiceId] });
  void queryClient.invalidateQueries({ queryKey: ["invoices"] });
  void queryClient.invalidateQueries({ queryKey: ["payments"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  void queryClient.invalidateQueries({ queryKey: ["activity"] });
}

export function InvoiceDetailPage() {
  const { t } = useTranslation();
  const currency = useCompanyCurrency();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();

  const [paymentFormOpen, setPaymentFormOpen] = useState(false);
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState<PaymentMethod>(PaymentMethod.BankTransfer);
  const [paymentDate, setPaymentDate] = useState(todayInputValue);
  const [reference, setReference] = useState("");
  const [notes, setNotes] = useState("");
  const [paymentError, setPaymentError] = useState<string | null>(null);

  const { data: invoice, isLoading, error } = useQuery({
    queryKey: ["invoice", id],
    enabled: Boolean(id),
    queryFn: () =>
      managementRequest<InvoiceDetail>(billingApi.invoice(id!), {
        schema: invoiceDetailSchema,
      }),
  });

  const { data: payments } = useQuery({
    queryKey: ["invoice-payments", id],
    enabled: Boolean(id),
    queryFn: () =>
      managementRequest<PaymentRecord[]>(billingApi.invoicePayments(id!), {
        schema: z.array(paymentRecordSchema),
      }),
  });

  const completedPaid = useMemo(
    () =>
      (payments ?? [])
        .filter((p) => p.status === PaymentStatus.Completed)
        .reduce((sum, p) => sum + p.amount, 0),
    [payments],
  );

  const amountDue = invoice ? Math.max(0, Math.round((invoice.total - completedPaid) * 100) / 100) : 0;

  useEffect(() => {
    if (paymentFormOpen && amountDue > 0) {
      setAmount(String(amountDue));
    }
  }, [paymentFormOpen, amountDue]);

  const actionError = (err: unknown) => {
    toast(err instanceof ApiError ? err.message : t("invoices.actionError"), "error");
  };

  const sendMutation = useMutation({
    mutationFn: () =>
      managementRequest<InvoiceDetail>(billingApi.invoiceSend(id!), {
        method: "POST",
        schema: invoiceDetailSchema,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(["invoice", id], updated);
      invalidateBillingCaches(queryClient, id!);
      toast(t("toast.invoiceSent"), "success");
    },
    onError: actionError,
  });

  const emailMutation = useMutation({
    mutationFn: () =>
      managementRequest<{ message: string }>(billingApi.invoiceEmail(id!), {
        method: "POST",
        schema: messageSchema,
      }),
    onSuccess: (response) => {
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      const skipped = response.message.toLowerCase().includes("smtp");
      toast(
        skipped ? t("invoices.emailSkipped") : t("toast.invoiceEmailed"),
        skipped ? "info" : "success",
      );
    },
    onError: actionError,
  });

  const shareMutation = useMutation({
    mutationFn: () =>
      managementRequest<z.infer<typeof shareLinkSchema>>(billingApi.invoiceShareLink(id!), {
        method: "POST",
        schema: shareLinkSchema,
      }),
    onSuccess: async (data) => {
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      if (data.alreadyActive) {
        toast(t("invoices.shareLinkAlreadyActive"), "info");
        return;
      }

      const portalUrl = data.url ?? `${window.location.origin}/portal/${data.token ?? ""}`;
      try {
        await navigator.clipboard.writeText(portalUrl);
        toast(t("invoices.shareLinkCopied"), "success");
      } catch {
        toast(t("invoices.shareLinkCopyFailed"), "error");
      }
    },
    onError: actionError,
  });

  const revokeMutation = useMutation({
    mutationFn: () =>
      managementRequest<{ message: string }>(billingApi.invoiceShareLink(id!), {
        method: "DELETE",
        schema: messageSchema,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("invoices.shareLinkRevoked"), "success");
    },
    onError: actionError,
  });

  const markPaidMutation = useMutation({
    mutationFn: () =>
      managementRequest<InvoiceDetail>(billingApi.invoiceMarkPaid(id!), {
        method: "POST",
        schema: invoiceDetailSchema,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(["invoice", id], updated);
      invalidateBillingCaches(queryClient, id!);
      setPaymentFormOpen(false);
      toast(t("toast.invoiceMarkedPaid"), "success");
    },
    onError: actionError,
  });

  const cancelMutation = useMutation({
    mutationFn: () =>
      managementRequest<InvoiceDetail>(billingApi.invoiceCancel(id!), {
        method: "POST",
        schema: invoiceDetailSchema,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(["invoice", id], updated);
      invalidateBillingCaches(queryClient, id!);
      toast(t("toast.invoiceCancelled"), "success");
    },
    onError: actionError,
  });

  const duplicateMutation = useMutation({
    mutationFn: () =>
      managementRequest<InvoiceDetail>(billingApi.invoiceDuplicate(id!), {
        method: "POST",
        schema: invoiceDetailSchema,
      }),
    onSuccess: (created) => {
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("toast.invoiceDuplicated"), "success");
      navigate(`/invoices/${created.id}`, { replace: true });
    },
    onError: actionError,
  });

  const deleteMutation = useMutation({
    mutationFn: () =>
      managementRequest<{ message: string }>(billingApi.invoice(id!), {
        method: "DELETE",
        schema: messageSchema,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      toast(t("toast.invoiceDeleted"), "success");
      navigate("/invoices", { replace: true });
    },
    onError: actionError,
  });

  const recordPaymentMutation = useMutation({
    mutationFn: (body: {
      invoiceId: string;
      amount: number;
      method: PaymentMethod;
      paymentDate?: string;
      reference?: string;
      notes?: string;
    }) =>
      managementRequest<PaymentRecord>(billingApi.payments, {
        method: "POST",
        body,
        schema: paymentRecordSchema,
      }),
    onSuccess: () => {
      invalidateBillingCaches(queryClient, id!);
      setPaymentFormOpen(false);
      setReference("");
      setNotes("");
      setPaymentError(null);
      toast(t("toast.paymentRecorded"), "success");
    },
    onError: (err) => {
      const message = err instanceof ApiError ? err.message : t("invoices.actionError");
      setPaymentError(message);
      toast(message, "error");
    },
  });

  const refundMutation = useMutation({
    mutationFn: (paymentId: string) =>
      managementRequest<PaymentRecord>(billingApi.paymentRefund(paymentId), {
        method: "POST",
        schema: paymentRecordSchema,
      }),
    onSuccess: () => {
      invalidateBillingCaches(queryClient, id!);
      toast(t("toast.paymentRefunded"), "success");
    },
    onError: actionError,
  });

  const cancelPaymentMutation = useMutation({
    mutationFn: (paymentId: string) =>
      managementRequest<PaymentRecord>(billingApi.paymentCancel(paymentId), {
        method: "POST",
        schema: paymentRecordSchema,
      }),
    onSuccess: () => {
      invalidateBillingCaches(queryClient, id!);
      toast(t("toast.paymentCancelled"), "success");
    },
    onError: actionError,
  });

  const downloadPdf = async () => {
    if (!id) return;
    try {
      await downloadWithAuth(
        billingApi.invoicePdf(id),
        `${invoice?.invoiceNumber ?? "invoice"}.pdf`,
      );
    } catch {
      toast(t("invoices.actionError"), "error");
    }
  };

  const submitPayment = () => {
    if (!id || !invoice) return;
    const parsed = Number(amount);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setPaymentError(t("payments.amountRequired"));
      return;
    }
    if (parsed > amountDue + 0.001) {
      setPaymentError(t("payments.amountExceedsDue"));
      return;
    }

    recordPaymentMutation.mutate({
      invoiceId: id,
      amount: parsed,
      method,
      paymentDate: paymentDate || undefined,
      reference: reference.trim() || undefined,
      notes: notes.trim() || undefined,
    });
  };

  const anyActionPending =
    sendMutation.isPending
    || emailMutation.isPending
    || shareMutation.isPending
    || revokeMutation.isPending
    || markPaidMutation.isPending
    || cancelMutation.isPending
    || duplicateMutation.isPending
    || deleteMutation.isPending
    || recordPaymentMutation.isPending;

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
            <StatusBadge label={invoiceStatusLabel(invoice.status, t)} variant={statusVariant(invoice.status)} />
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
              <span>{formatMoney(invoice.subtotal, currency)}</span>
            </div>
            <div className="invoice-total-row">
              <span>{t("invoices.tax", { rate: invoice.taxRate })}</span>
              <span>{formatMoney(invoice.taxAmount, currency)}</span>
            </div>
            <div className="invoice-total-row invoice-total-row--grand">
              <span>{t("invoices.total")}</span>
              <span>{formatMoney(invoice.total, currency)}</span>
            </div>
            {canReceivePayment(invoice.status) || invoice.status === InvoiceStatus.Paid ? (
              <div className="invoice-total-row">
                <span>{t("invoices.amountDue")}</span>
                <span className="font-semibold text-accent">{formatMoney(amountDue, currency)}</span>
              </div>
            ) : null}
          </div>

          <div className="detail-section">
            <h3 className="detail-section-title">{t("invoices.lineItems")}</h3>
            <ul className="list-stack">
              {invoice.lineItems.map((item) => (
                <li key={item.id} className="card list-row-static">
                  <p className="font-medium">{item.description}</p>
                  <p className="text-sm text-secondary">
                    {item.quantity} × {formatMoney(item.unitPrice, currency)}
                  </p>
                  <p className="text-right font-semibold text-accent">
                    {formatMoney(item.lineTotal, currency)}
                  </p>
                </li>
              ))}
            </ul>
          </div>

          {(payments?.length ?? 0) > 0 ? (
            <div className="detail-section">
              <h3 className="detail-section-title">{t("invoices.paymentHistory")}</h3>
              <ul className="list-stack">
                {payments!.map((payment) => (
                  <li key={payment.id} className="card list-row-static">
                    <div className="flex justify-between gap-2">
                      <span className="font-medium">{formatMoney(payment.amount, currency)}</span>
                      <StatusBadge
                        label={paymentStatusLabel(payment.status, t)}
                        variant={paymentBadgeVariant(payment.status)}
                      />
                    </div>
                    <p className="text-sm text-secondary">
                      {paymentMethodLabel(payment.method, t)} · {formatDate(payment.paymentDate)}
                    </p>
                    {payment.status === PaymentStatus.Completed ? (
                      <div className="mt-2 flex flex-wrap gap-2">
                        <button
                          className="btn-ghost text-sm"
                          disabled={refundMutation.isPending || cancelPaymentMutation.isPending}
                          onClick={() => {
                            if (window.confirm(t("payments.refundConfirm"))) {
                              refundMutation.mutate(payment.id);
                            }
                          }}
                          type="button"
                        >
                          {t("payments.refund")}
                        </button>
                        <button
                          className="btn-ghost text-sm text-red-500"
                          disabled={refundMutation.isPending || cancelPaymentMutation.isPending}
                          onClick={() => {
                            if (window.confirm(t("payments.cancelConfirm"))) {
                              cancelPaymentMutation.mutate(payment.id);
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
            </div>
          ) : null}

          {paymentFormOpen && canReceivePayment(invoice.status) ? (
            <div className="card space-y-3">
              <h3 className="detail-section-title">{t("payments.record")}</h3>
              <FormField
                label={t("payments.amount")}
                type="number"
                min={0.01}
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
              <label className="block space-y-1.5 text-sm">
                <span className="font-medium text-primary">{t("payments.method")}</span>
                <select
                  className="field-select"
                  value={method}
                  onChange={(e) => setMethod(Number(e.target.value) as PaymentMethod)}
                >
                  {PAYMENT_METHODS.map((m) => (
                    <option key={m} value={m}>
                      {paymentMethodLabel(m, t)}
                    </option>
                  ))}
                </select>
              </label>
              <FormField
                label={t("payments.date")}
                type="date"
                value={paymentDate}
                onChange={(e) => setPaymentDate(e.target.value)}
              />
              <FormField
                label={t("payments.reference")}
                value={reference}
                onChange={(e) => setReference(e.target.value)}
              />
              <FormTextArea
                label={t("payments.notes")}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
              />
              {paymentError ? <p className="text-sm text-red-500">{paymentError}</p> : null}
              <div className="flex flex-wrap gap-2">
                <button
                  className="btn-primary"
                  disabled={recordPaymentMutation.isPending}
                  onClick={submitPayment}
                  type="button"
                >
                  {recordPaymentMutation.isPending ? t("payments.saving") : t("payments.save")}
                </button>
                <button
                  className="btn-secondary"
                  onClick={() => {
                    setPaymentFormOpen(false);
                    setPaymentError(null);
                  }}
                  type="button"
                >
                  {t("payments.cancel")}
                </button>
              </div>
            </div>
          ) : null}

          <div className="detail-actions flex-wrap">
            {invoice.status === InvoiceStatus.Draft ? (
              <>
                <button
                  className="btn-primary flex flex-1 items-center justify-center gap-2"
                  disabled={anyActionPending}
                  onClick={() => void sendMutation.mutate()}
                  type="button"
                >
                  <Send className="h-4 w-4" />
                  {sendMutation.isPending ? t("app.loading") : t("invoices.send")}
                </button>
                <Link
                  className="btn-secondary flex flex-1 items-center justify-center gap-2 no-underline"
                  to={`/invoices/${id}/edit`}
                >
                  <Pencil className="h-4 w-4" />
                  {t("invoices.editDraft")}
                </Link>
              </>
            ) : null}

            {canReceivePayment(invoice.status) ? (
              <button
                className="btn-primary flex flex-1 items-center justify-center gap-2"
                disabled={anyActionPending}
                onClick={() => setPaymentFormOpen((open) => !open)}
                type="button"
              >
                <Banknote className="h-4 w-4" />
                {paymentFormOpen ? t("invoices.hidePaymentForm") : t("invoices.recordPayment")}
              </button>
            ) : null}

            {canMarkInvoicePaid(invoice.status) ? (
              <button
                className="btn-secondary flex flex-1 items-center justify-center gap-2"
                disabled={anyActionPending}
                onClick={() => {
                  if (window.confirm(t("invoices.markPaidConfirm"))) {
                    markPaidMutation.mutate();
                  }
                }}
                type="button"
              >
                <CheckCircle2 className="h-4 w-4" />
                {markPaidMutation.isPending ? t("app.loading") : t("invoices.markPaid")}
              </button>
            ) : null}

            {canEmailInvoice(invoice.status) ? (
              <button
                className="btn-secondary flex flex-1 items-center justify-center gap-2"
                disabled={anyActionPending}
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

            <button
              className="btn-secondary flex flex-1 items-center justify-center gap-2"
              disabled={anyActionPending}
              onClick={() => void duplicateMutation.mutate()}
              type="button"
            >
              <Copy className="h-4 w-4" />
              {duplicateMutation.isPending ? t("app.loading") : t("invoices.duplicate")}
            </button>

            {invoice.status !== InvoiceStatus.Draft && invoice.status !== InvoiceStatus.Cancelled ? (
              <>
                <button
                  className="btn-secondary flex flex-1 items-center justify-center gap-2"
                  disabled={anyActionPending}
                  onClick={() => void shareMutation.mutate()}
                  type="button"
                >
                  <Link2 className="h-4 w-4" />
                  {shareMutation.isPending ? t("app.loading") : t("invoices.shareLink")}
                </button>
                <button
                  className="btn-ghost flex flex-1 items-center justify-center gap-2 text-sm"
                  disabled={anyActionPending}
                  onClick={() => void revokeMutation.mutate()}
                  type="button"
                >
                  <Unlink className="h-4 w-4" />
                  {revokeMutation.isPending ? t("app.loading") : t("invoices.revokeLink")}
                </button>
              </>
            ) : null}

            {canCancelInvoice(invoice.status) ? (
              <button
                className="btn-ghost flex flex-1 items-center justify-center gap-2 text-sm text-red-500"
                disabled={anyActionPending}
                onClick={() => {
                  if (window.confirm(t("invoices.cancelConfirm"))) {
                    cancelMutation.mutate();
                  }
                }}
                type="button"
              >
                <XCircle className="h-4 w-4" />
                {cancelMutation.isPending ? t("app.loading") : t("invoices.cancel")}
              </button>
            ) : null}

            {canDeleteInvoice(invoice.status) ? (
              <button
                className="btn-ghost flex flex-1 items-center justify-center gap-2 text-sm text-red-500"
                disabled={anyActionPending}
                onClick={() => {
                  if (window.confirm(t("invoices.deleteConfirm"))) {
                    deleteMutation.mutate();
                  }
                }}
                type="button"
              >
                <Trash2 className="h-4 w-4" />
                {deleteMutation.isPending ? t("app.loading") : t("invoices.deleteDraft")}
              </button>
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
