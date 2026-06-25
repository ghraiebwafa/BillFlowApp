import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { FormField } from "../../../shared/ui/FormField";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import type { ClientResponse } from "../../../domain/billing/client";
import type { InvoiceDetail } from "../../../domain/billing/invoice";

type CreateForm = {
  clientId: string;
  taxRate: number;
  notes: string;
  itemDescription: string;
  quantity: number;
  unitPrice: number;
};

export function CreateInvoicePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [formError, setFormError] = useState<string | null>(null);
  const [step, setStep] = useState<"billTo" | "items" | "summary">("billTo");

  const { data: clients } = useQuery({
    queryKey: ["clients"],
    queryFn: () => managementRequest<ClientResponse[]>("/api/v1.0/billing/Client/GetAll"),
  });

  const { register, handleSubmit, watch, getValues } = useForm<CreateForm>({
    defaultValues: {
      clientId: "",
      taxRate: 10,
      notes: "",
      itemDescription: "",
      quantity: 1,
      unitPrice: 0,
    },
  });

  const createMutation = useMutation({
    mutationFn: (body: unknown) =>
      managementRequest<InvoiceDetail>("/api/v1.0/billing/Invoice/Create", {
        method: "POST",
        body,
      }),
  });

  const selectedClientId = watch("clientId");
  const selectedClient = clients?.find((c) => c.id === selectedClientId);

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
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : t("invoices.createErrors.generic"));
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
            onClick={() => setStep(key)}
            type="button"
          >
            {label}
          </button>
        ))}
      </div>

      <form className="card space-y-4" onSubmit={onCreate}>
        {step === "billTo" ? (
          <>
            <label className="block space-y-1.5 text-sm">
              <span className="font-medium">{t("invoices.client")}</span>
              <select className="field-select" {...register("clientId")}>
                <option value="">{t("invoices.selectClient")}</option>
                {(clients ?? []).map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.companyName}
                  </option>
                ))}
              </select>
            </label>
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
              <span className="font-medium">{getValues("unitPrice")}</span>
            </p>
          </div>
        ) : null}

        {formError ? <p className="text-sm text-red-500">{formError}</p> : null}

        <div className="flex gap-2">
          {step !== "billTo" ? (
            <button
              className="btn-secondary flex-1"
              onClick={() => setStep(step === "summary" ? "items" : "billTo")}
              type="button"
            >
              {t("common.back")}
            </button>
          ) : null}
          {step !== "summary" ? (
            <button
              className="btn-primary flex-1"
              onClick={() => setStep(step === "billTo" ? "items" : "summary")}
              type="button"
            >
              {t("common.next")}
            </button>
          ) : (
            <button className="btn-primary btn-primary--lg flex-1" disabled={createMutation.isPending} type="submit">
              {createMutation.isPending ? t("invoices.creating") : t("invoices.createSubmit")}
            </button>
          )}
        </div>
      </form>

      {!clients?.length ? (
        <p className="text-center text-sm text-secondary">
          <Link to="/clients" className="text-accent no-underline">
            {t("invoices.addClientFirst")}
          </Link>
        </p>
      ) : null}
    </section>
  );
}
