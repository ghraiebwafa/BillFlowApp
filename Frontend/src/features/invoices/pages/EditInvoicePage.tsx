import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { FormField } from "../../../shared/ui/FormField";
import { FormTextArea } from "../../../shared/ui/FormTextArea";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import type { ClientResponse } from "../../../domain/billing/client";
import type { ItemResponse } from "../../../domain/billing/item";
import {
  InvoiceStatus,
  type InvoiceDetail,
} from "../../../domain/billing/invoice";
import { buildPageQuery, pagedSchema, type PagedResponse } from "../../../domain/billing/paging";
import {
  clientResponseSchema,
  invoiceDetailSchema,
  itemResponseSchema,
} from "../../../domain/billing/schemas";
import { toast } from "../../../shared/ui/toast-store";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";

const PICKER_PAGE_SIZE = 50;
const pagedClientSchema = pagedSchema(clientResponseSchema);
const pagedItemSchema = pagedSchema(itemResponseSchema);

type LineDraft = {
  itemId: string;
  description: string;
  quantity: number;
  unitPrice: number;
};

function toDateInput(value: string): string {
  return value.slice(0, 10);
}

export function EditInvoicePage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const currency = useCompanyCurrency();

  const [clientId, setClientId] = useState("");
  const [taxRate, setTaxRate] = useState(0);
  const [notes, setNotes] = useState("");
  const [invoiceDate, setInvoiceDate] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [line, setLine] = useState<LineDraft>({
    itemId: "",
    description: "",
    quantity: 1,
    unitPrice: 0,
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [hydrated, setHydrated] = useState(false);
  const [clientSearch, setClientSearch] = useState("");
  const [itemSearch, setItemSearch] = useState("");
  const debouncedClientSearch = useDebouncedValue(clientSearch);
  const debouncedItemSearch = useDebouncedValue(itemSearch);

  const invoiceQuery = useQuery({
    queryKey: ["invoice", id],
    enabled: Boolean(id),
    queryFn: () =>
      managementRequest<InvoiceDetail>(billingApi.invoice(id!), {
        schema: invoiceDetailSchema,
      }),
  });

  const clientsQuery = useQuery({
    queryKey: ["clients", { mode: "picker", search: debouncedClientSearch, page: 1, pageSize: PICKER_PAGE_SIZE }],
    queryFn: () =>
      managementRequest<PagedResponse<ClientResponse>>(
        `${billingApi.clients}${buildPageQuery({
          search: debouncedClientSearch,
          page: 1,
          pageSize: PICKER_PAGE_SIZE,
        })}`,
        { schema: pagedClientSchema },
      ),
  });

  const itemsQuery = useQuery({
    queryKey: ["items", { mode: "picker", search: debouncedItemSearch, page: 1, pageSize: PICKER_PAGE_SIZE }],
    queryFn: () =>
      managementRequest<PagedResponse<ItemResponse>>(
        `${billingApi.items}${buildPageQuery({
          search: debouncedItemSearch,
          page: 1,
          pageSize: PICKER_PAGE_SIZE,
        })}`,
        { schema: pagedItemSchema },
      ),
  });

  const invoice = invoiceQuery.data;
  const clients = (clientsQuery.data?.items ?? []).filter((c) => c.isActive);
  const catalogItems = (itemsQuery.data?.items ?? []).filter((item) => item.isActive && !item.isArchived);

  useEffect(() => {
    if (!invoice || hydrated) return;
    if (invoice.status !== InvoiceStatus.Draft) {
      navigate(`/invoices/${invoice.id}`, { replace: true });
      return;
    }
    const first = invoice.lineItems[0];
    setClientId(invoice.clientId);
    setTaxRate(invoice.taxRate);
    setNotes(invoice.notes ?? "");
    setInvoiceDate(toDateInput(invoice.invoiceDate));
    setDueDate(toDateInput(invoice.dueDate));
    setLine({
      itemId: first?.itemId ?? "",
      description: first?.description ?? "",
      quantity: first?.quantity ?? 1,
      unitPrice: first?.unitPrice ?? 0,
    });
    setHydrated(true);
  }, [invoice, hydrated, navigate]);

  const saveMutation = useMutation({
    mutationFn: (body: unknown) =>
      managementRequest<InvoiceDetail>(billingApi.invoice(id!), {
        method: "PUT",
        body,
        schema: invoiceDetailSchema,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(["invoice", id], updated);
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      toast(t("common.saved"), "success");
      navigate(`/invoices/${updated.id}`, { replace: true });
    },
    onError: (err) => {
      const message = err instanceof ApiError ? err.message : t("invoices.actionError");
      setFormError(message);
      toast(message, "error");
    },
  });

  const applyCatalogItem = (itemId: string) => {
    if (!itemId) {
      setLine((prev) => ({ ...prev, itemId: "" }));
      return;
    }
    const item = catalogItems.find((entry) => entry.id === itemId);
    if (!item) return;
    setLine({
      itemId,
      description: item.name,
      quantity: line.quantity || 1,
      unitPrice: item.unitPrice,
    });
  };

  const onSave = () => {
    setFormError(null);
    if (!clientId) {
      setFormError(t("invoices.createErrors.clientRequired"));
      return;
    }
    if (!line.description.trim() || line.unitPrice <= 0 || line.quantity < 1) {
      setFormError(t("invoices.createErrors.itemRequired"));
      return;
    }

    saveMutation.mutate({
      clientId,
      invoiceDate,
      dueDate,
      taxRate,
      notes: notes.trim() || null,
      lineItems: [
        {
          itemId: line.itemId || null,
          description: line.description.trim(),
          quantity: line.quantity,
          unitPrice: line.unitPrice,
        },
      ],
    });
  };

  if (!id) return null;

  if (invoiceQuery.isLoading || !hydrated) {
    return (
      <section className="app-screen">
        <PageHeader title={t("invoices.edit")} backTo={`/invoices/${id}`} />
        <div className="card">{t("app.loading")}</div>
      </section>
    );
  }

  if (invoiceQuery.error || !invoice) {
    return (
      <section className="app-screen">
        <PageHeader title={t("invoices.edit")} backTo="/invoices" />
        <div className="card text-red-500">
          {invoiceQuery.error instanceof ApiError
            ? invoiceQuery.error.message
            : t("invoices.loadError")}
        </div>
      </section>
    );
  }

  return (
    <section className="app-screen">
      <PageHeader
        title={`${t("invoices.editDraft")} · ${invoice.invoiceNumber}`}
        backTo={`/invoices/${id}`}
      />

      <div className="card space-y-4">
        <FormField
          label={t("invoices.searchClients")}
          value={clientSearch}
          onChange={(e) => setClientSearch(e.target.value)}
          placeholder={t("clients.searchPlaceholder")}
        />
        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">{t("invoices.client")}</span>
          <select className="field-select" value={clientId} onChange={(e) => setClientId(e.target.value)}>
            <option value="">{t("invoices.selectClient")}</option>
            {clients.map((client) => (
              <option key={client.id} value={client.id}>
                {client.companyName}
              </option>
            ))}
            {clientId && !clients.some((c) => c.id === clientId) ? (
              <option value={clientId}>{invoice.clientCompanyName}</option>
            ) : null}
          </select>
        </label>

        <div className="detail-grid">
          <FormField
            label={t("invoices.issueDate")}
            type="date"
            value={invoiceDate}
            onChange={(e) => setInvoiceDate(e.target.value)}
          />
          <FormField
            label={t("invoices.dueDate")}
            type="date"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </div>

        <FormField
          label={t("invoices.taxRate")}
          type="number"
          step="0.01"
          value={taxRate}
          onChange={(e) => setTaxRate(Number(e.target.value))}
        />

        <FormField
          label={t("invoices.searchCatalog")}
          value={itemSearch}
          onChange={(e) => setItemSearch(e.target.value)}
          placeholder={t("items.searchPlaceholder")}
        />
        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">{t("invoices.pickCatalogItem")}</span>
          <select
            className="field-select"
            value={line.itemId}
            onChange={(e) => applyCatalogItem(e.target.value)}
          >
            <option value="">{t("invoices.manualLineItem")}</option>
            {catalogItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name} — {formatMoney(item.unitPrice, item.currency || currency)}
              </option>
            ))}
          </select>
        </label>

        <FormField
          label={t("invoices.itemDescription")}
          value={line.description}
          onChange={(e) => setLine((prev) => ({ ...prev, description: e.target.value }))}
        />
        <FormField
          label={t("invoices.quantity")}
          type="number"
          min={1}
          value={line.quantity}
          onChange={(e) => setLine((prev) => ({ ...prev, quantity: Number(e.target.value) }))}
        />
        <FormField
          label={t("invoices.unitPrice")}
          type="number"
          step="0.01"
          min={0}
          value={line.unitPrice}
          onChange={(e) => setLine((prev) => ({ ...prev, unitPrice: Number(e.target.value) }))}
        />
        <FormTextArea
          label={t("payments.notes")}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />

        {formError ? (
          <p className="text-sm text-red-500" role="alert">
            {formError}
          </p>
        ) : null}

        <div className="flex gap-2">
          <Link className="btn-secondary flex-1 text-center no-underline" to={`/invoices/${id}`}>
            {t("admin.cancel")}
          </Link>
          <button
            className="btn-primary flex-1"
            type="button"
            disabled={saveMutation.isPending}
            onClick={onSave}
          >
            {saveMutation.isPending ? t("invoices.saving") : t("invoices.saveChanges")}
          </button>
        </div>
      </div>
    </section>
  );
}
