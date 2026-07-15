import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { FormField } from "../../../shared/ui/FormField";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import type { ClientResponse } from "../../../domain/billing/client";
import type { InvoiceDetail } from "../../../domain/billing/invoice";
import { buildPageQuery, pagedSchema, type PagedResponse } from "../../../domain/billing/paging";
import { clientResponseSchema, invoiceDetailSchema } from "../../../domain/billing/schemas";
import { toast } from "../../../shared/ui/toast-store";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";
import {
  companySettingsQueryKey,
  fetchCompanySettings,
} from "../../../domain/billing/company-settings-api";

type CreateForm = {
  clientId: string;
  taxRate: number;
  notes: string;
  itemDescription: string;
  quantity: number;
  unitPrice: number;
};

type StepKey = "billTo" | "items" | "summary";

const PICKER_PAGE_SIZE = 50;
const pagedClientSchema = pagedSchema(clientResponseSchema);
const stepOrder: StepKey[] = ["billTo", "items", "summary"];

export function CreateInvoicePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const currency = useCompanyCurrency();
  const [formError, setFormError] = useState<string | null>(null);
  const [step, setStep] = useState<StepKey>("billTo");
  const [clientSearch, setClientSearch] = useState("");
  const debouncedClientSearch = useDebouncedValue(clientSearch);

  const {
    data: clientsPage,
    isLoading: clientsLoading,
    error: clientsError,
  } = useQuery({
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

  const { data: companySettings } = useQuery({
    queryKey: companySettingsQueryKey,
    queryFn: fetchCompanySettings,
    staleTime: 60_000,
  });

  const clients = (clientsPage?.items ?? []).filter((client) => client.isActive);
  const clientsTruncated = (clientsPage?.totalCount ?? 0) > PICKER_PAGE_SIZE;

  const { register, handleSubmit, watch, getValues, setValue } = useForm<CreateForm>({
    defaultValues: {
      clientId: "",
      taxRate: 0,
      notes: "",
      itemDescription: "",
      quantity: 1,
      unitPrice: 0,
    },
  });

  useEffect(() => {
    if (companySettings?.defaultTaxRate != null) {
      setValue("taxRate", companySettings.defaultTaxRate);
    }
  }, [companySettings, setValue]);

  const selectedClientId = watch("clientId");

  useEffect(() => {
    if (!selectedClientId) return;
    if (!clients.some((client) => client.id === selectedClientId)) {
      setValue("clientId", "");
    }
  }, [clients, selectedClientId, setValue]);

  const createMutation = useMutation({
    mutationFn: (body: unknown) =>
      managementRequest<InvoiceDetail>(billingApi.invoices, {
        method: "POST",
        body,
        schema: invoiceDetailSchema,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["invoices"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      void queryClient.invalidateQueries({ queryKey: ["activity"] });
    },
  });

  const canEnterStep = (target: StepKey): boolean => {
    const values = getValues();
    if (target === "billTo") return true;
    if (!values.clientId) {
      setFormError(t("invoices.createErrors.clientRequired"));
      return false;
    }
    if (target === "items") return true;
    if (!values.itemDescription.trim() || values.unitPrice <= 0 || values.quantity < 1) {
      setFormError(t("invoices.createErrors.itemRequired"));
      return false;
    }
    return true;
  };

  const goNext = () => {
    setFormError(null);
    const next = step === "billTo" ? "items" : "summary";
    if (!canEnterStep(next)) return;
    setStep(next);
  };

  const goBack = () => {
    setFormError(null);
    setStep(step === "summary" ? "items" : "billTo");
  };

  const selectStep = (target: StepKey) => {
    setFormError(null);
    const currentIndex = stepOrder.indexOf(step);
    const targetIndex = stepOrder.indexOf(target);
    if (targetIndex <= currentIndex) {
      setStep(target);
      return;
    }
    if (!canEnterStep(target)) return;
    setStep(target);
  };

  const selectedClient = clients.find((c) => c.id === selectedClientId);

  const onCreate = handleSubmit(async (values) => {
    setFormError(null);
    if (!values.clientId) {
      setFormError(t("invoices.createErrors.clientRequired"));
      return;
    }
    if (!values.itemDescription.trim() || values.unitPrice <= 0) {
      setFormError(t("invoices.createErrors.itemRequired"));
      return;
    }

    try {
      const result = await createMutation.mutateAsync({
        clientId: values.clientId,
        taxRate: values.taxRate,
        notes: values.notes || null,
        lineItems: [
          {
            description: values.itemDescription.trim(),
            quantity: values.quantity,
            unitPrice: values.unitPrice,
          },
        ],
      });
      navigate(`/invoices/${result.id}`, { replace: true });
      toast(t("toast.invoiceCreated"), "success");
    } catch (error) {
      const message = error instanceof ApiError ? error.message : t("invoices.createErrors.generic");
      setFormError(message);
      toast(message, "error");
    }
  });

  const steps = [
    { key: "billTo" as const, label: t("invoices.steps.billTo") },
    { key: "items" as const, label: t("invoices.steps.items") },
    { key: "summary" as const, label: t("invoices.steps.summary") },
  ];

  return (
    <section className="app-screen">
      <PageHeader title={t("invoices.create")} backTo="/invoices" />

      <div className="step-tabs">
        {steps.map(({ key, label }) => (
          <button
            key={key}
            className={step === key ? "step-tab active" : "step-tab"}
            onClick={() => selectStep(key)}
            type="button"
            aria-pressed={step === key}
          >
            {label}
          </button>
        ))}
      </div>

      <form className="card space-y-4" onSubmit={onCreate}>
        {step === "billTo" ? (
          <>
            <FormField
              label={t("invoices.searchClients")}
              value={clientSearch}
              onChange={(e) => setClientSearch(e.target.value)}
              placeholder={t("clients.searchPlaceholder")}
            />
            {clientsLoading ? <p className="text-sm text-secondary">{t("app.loading")}</p> : null}
            {clientsError ? (
              <p className="text-sm text-red-500" role="alert">
                {clientsError instanceof ApiError ? clientsError.message : t("clients.loadError")}
              </p>
            ) : null}
            {!clientsLoading && !clientsError ? (
              <label className="block space-y-1.5 text-sm">
                <span className="font-medium">{t("invoices.client")}</span>
                <select className="field-select" {...register("clientId")}>
                  <option value="">{t("invoices.selectClient")}</option>
                  {clients.map((client) => (
                    <option key={client.id} value={client.id}>
                      {client.companyName}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
            {clientsTruncated ? (
              <p className="text-xs text-secondary">{t("invoices.clientSearchHint")}</p>
            ) : null}
            <FormField label={t("invoices.taxRate")} type="number" step="0.01" {...register("taxRate", { valueAsNumber: true })} />
          </>
        ) : null}

        {step === "items" ? (
          <>
            <FormField label={t("invoices.itemDescription")} {...register("itemDescription")} />
            <FormField label={t("invoices.quantity")} type="number" min={1} {...register("quantity", { valueAsNumber: true })} />
            <FormField label={t("invoices.unitPrice")} type="number" step="0.01" min={0} {...register("unitPrice", { valueAsNumber: true })} />
          </>
        ) : null}

        {step === "summary" ? (
          <div className="space-y-2 text-sm">
            <p>
              <span className="text-secondary">{t("invoices.client")}: </span>
              <span className="font-medium">{selectedClient?.companyName ?? "—"}</span>
            </p>
            <p>
              <span className="text-secondary">{t("invoices.itemDescription")}: </span>
              <span className="font-medium">{getValues("itemDescription") || "—"}</span>
            </p>
            <p>
              <span className="text-secondary">{t("invoices.quantity")}: </span>
              <span className="font-medium">{getValues("quantity")}</span>
            </p>
            <p>
              <span className="text-secondary">{t("invoices.unitPrice")}: </span>
              <span className="font-medium">{formatMoney(getValues("unitPrice") || 0, currency)}</span>
            </p>
          </div>
        ) : null}

        {formError ? (
          <p className="text-sm text-red-500" role="alert">
            {formError}
          </p>
        ) : null}

        <div className="flex gap-2">
          {step !== "billTo" ? (
            <button className="btn-secondary flex-1" onClick={goBack} type="button">
              {t("common.back")}
            </button>
          ) : null}
          {step !== "summary" ? (
            <button className="btn-primary flex-1" onClick={goNext} type="button">
              {t("common.next")}
            </button>
          ) : (
            <button className="btn-primary btn-primary--lg flex-1" disabled={createMutation.isPending} type="submit">
              {createMutation.isPending ? t("invoices.creating") : t("invoices.createSubmit")}
            </button>
          )}
        </div>
      </form>

      {!clientsLoading && !clientsError && clients.length === 0 ? (
        <p className="text-center text-sm text-secondary">
          <Link to="/clients" className="text-accent no-underline">
            {t("invoices.addClientFirst")}
          </Link>
        </p>
      ) : null}
    </section>
  );
}
