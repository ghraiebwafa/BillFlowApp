import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { FormField } from "../../../shared/ui/FormField";
import { FormTextArea } from "../../../shared/ui/FormTextArea";
import {
  defaultCompanySettingsForm,
  mapFormToRequest,
  mapSettingsToForm,
  type CompanySettingsResponse,
} from "../../../domain/billing/company-settings";

const companySettingsSchema = z.object({
  companyName: z.string().trim().min(1).max(200),
  address: z.string().max(500),
  country: z.string().max(100),
  taxNumber: z.string().max(50),
  phoneNumber: z.string().max(30),
  email: z.union([z.literal(""), z.string().trim().email().max(150)]),
  currency: z.string().trim().min(3).max(3),
  invoiceNumberPrefix: z.string().trim().min(1).max(20),
  defaultTaxRate: z.number().min(0).max(100),
  paymentTermsDays: z.number().int().min(1).max(365),
  timeZone: z.string().max(100),
});

type CompanySettingsForm = z.infer<typeof companySettingsSchema>;

async function fetchCompanySettings(): Promise<CompanySettingsResponse | null> {
  try {
    return await managementRequest<CompanySettingsResponse>(
      "/api/v1.0/billing/CompanySettings/Get",
    );
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export function CompanySettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);
  const [savedMessage, setSavedMessage] = useState<string | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ["company-settings"],
    queryFn: fetchCompanySettings,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<CompanySettingsForm>({
    resolver: zodResolver(companySettingsSchema),
    defaultValues: defaultCompanySettingsForm,
  });

  useEffect(() => {
    if (data) {
      reset(mapSettingsToForm(data));
    }
  }, [data, reset]);

  const saveMutation = useMutation({
    mutationFn: (values: CompanySettingsForm) =>
      managementRequest<CompanySettingsResponse>("/api/v1.0/billing/CompanySettings/Upsert", {
        method: "PUT",
        body: mapFormToRequest(values),
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(["company-settings"], saved);
      reset(mapSettingsToForm(saved));
      setFormError(null);
      setSavedMessage(t("settings.saved"));
    },
    onError: (mutationError) => {
      setSavedMessage(null);
      setFormError(
        mutationError instanceof ApiError ? mutationError.message : t("settings.saveError"),
      );
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setSavedMessage(null);
    setFormError(null);
    await saveMutation.mutateAsync(values);
  });

  const isNew = data === null && !isLoading && !error;

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-2xl font-semibold">{t("settings.title")}</h2>
        <p className="text-secondary">{t("settings.subtitle")}</p>
      </div>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}

      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("settings.loadError")}
        </div>
      ) : null}

      {!isLoading && !error ? (
        <form className="card space-y-6" onSubmit={onSubmit}>
          {isNew ? (
            <p className="rounded-md border border-accent/30 bg-accent/5 px-3 py-2 text-sm text-secondary">
              {t("settings.newHint")}
            </p>
          ) : null}

          <div className="grid gap-4 md:grid-cols-2">
            <FormField
              label={t("settings.fields.companyName")}
              error={errors.companyName?.message}
              {...register("companyName")}
            />
            <FormField
              label={t("settings.fields.email")}
              type="email"
              autoComplete="email"
              error={errors.email?.message}
              {...register("email")}
            />
            <div className="md:col-span-2">
              <FormTextArea
                label={t("settings.fields.address")}
                error={errors.address?.message}
                {...register("address")}
              />
            </div>
            <FormField
              label={t("settings.fields.country")}
              error={errors.country?.message}
              {...register("country")}
            />
            <FormField
              label={t("settings.fields.taxNumber")}
              error={errors.taxNumber?.message}
              {...register("taxNumber")}
            />
            <FormField
              label={t("settings.fields.phoneNumber")}
              type="tel"
              autoComplete="tel"
              error={errors.phoneNumber?.message}
              {...register("phoneNumber")}
            />
            <FormField
              label={t("settings.fields.timeZone")}
              placeholder="America/New_York"
              error={errors.timeZone?.message}
              {...register("timeZone")}
            />
          </div>

          <div>
            <h3 className="mb-3 text-lg font-medium">{t("settings.billingDefaults")}</h3>
            <div className="grid gap-4 md:grid-cols-2">
              <FormField
                label={t("settings.fields.currency")}
                maxLength={3}
                error={errors.currency?.message}
                {...register("currency")}
              />
              <FormField
                label={t("settings.fields.invoiceNumberPrefix")}
                error={errors.invoiceNumberPrefix?.message}
                {...register("invoiceNumberPrefix")}
              />
              <FormField
                label={t("settings.fields.defaultTaxRate")}
                type="number"
                step="0.01"
                min={0}
                max={100}
                error={errors.defaultTaxRate?.message}
                {...register("defaultTaxRate", { valueAsNumber: true })}
              />
              <FormField
                label={t("settings.fields.paymentTermsDays")}
                type="number"
                min={1}
                max={365}
                error={errors.paymentTermsDays?.message}
                {...register("paymentTermsDays", { valueAsNumber: true })}
              />
            </div>
          </div>

          {formError ? <p className="text-sm text-red-500">{formError}</p> : null}
          {savedMessage ? <p className="text-sm text-green-600">{savedMessage}</p> : null}

          <div className="flex justify-end">
            <button
              className="btn-primary"
              disabled={isSubmitting || saveMutation.isPending || (!isNew && !isDirty)}
              type="submit"
            >
              {isSubmitting || saveMutation.isPending ? t("settings.saving") : t("settings.save")}
            </button>
          </div>
        </form>
      ) : null}
    </section>
  );
}
