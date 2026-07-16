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
import { PageHeader } from "../../../shared/ui/PageHeader";
import { billingApi } from "../../../domain/billing/api-paths";
import {
  defaultCompanySettingsForm,
  mapFormToRequest,
  mapSettingsToForm,
  type CompanySettingsResponse,
} from "../../../domain/billing/company-settings";
import {
  companySettingsQueryKey,
  fetchCompanySettings,
} from "../../../domain/billing/company-settings-api";
import { env } from "../../../shared/config/env";
import { useSessionStore } from "../../../shared/auth/session-store";
import { toast } from "../../../shared/ui/toast-store";

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
  brandColor: z.union([z.literal(""), z.string().regex(/^#?[0-9A-Fa-f]{6}$/)]),
  invoiceFooterNote: z.string().max(500),
  enablePaymentReminders: z.boolean(),
  reminderDaysBeforeDue: z.number().int().min(0).max(30),
});

type CompanySettingsForm = z.infer<typeof companySettingsSchema>;

type SettingsTab = "general" | "invoice" | "payment" | "email";

async function uploadLogo(file: File): Promise<CompanySettingsResponse> {
  const { refreshSession, clearSession } = useSessionStore.getState();

  const execute = () => {
    const { accessToken } = useSessionStore.getState();
    if (!accessToken) throw new ApiError("Authentication required.", 401);
    const body = new FormData();
    body.append("file", file);
    return fetch(`${env.managementApiUrl}${billingApi.companySettingsLogo}`, {
      method: "POST",
      headers: { Authorization: `Bearer ${accessToken}` },
      body,
    });
  };

  let response = await execute();
  if (response.status === 401) {
    const refreshed = await refreshSession();
    if (!refreshed) {
      clearSession();
      throw new ApiError("Session expired.", 401);
    }
    response = await execute();
  }

  if (!response.ok) {
    throw new ApiError("Logo upload failed.", response.status);
  }

  return (await response.json()) as CompanySettingsResponse;
}

export function CompanySettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);
  const [savedMessage, setSavedMessage] = useState<string | null>(null);
  const [tab, setTab] = useState<SettingsTab>("general");
  const [logoBusy, setLogoBusy] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: companySettingsQueryKey,
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

  const hasLogo = Boolean(data?.hasLogo);

  useEffect(() => {
    if (data) {
      reset(mapSettingsToForm(data));
    }
  }, [data, reset]);

  const saveMutation = useMutation({
    mutationFn: (values: CompanySettingsForm) =>
      managementRequest<CompanySettingsResponse>(billingApi.companySettings, {
        method: "PUT",
        body: mapFormToRequest(values),
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(companySettingsQueryKey, saved);
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

  const onLogoSelected = async (file: File | undefined) => {
    if (!file) return;
    setLogoBusy(true);
    try {
      const saved = await uploadLogo(file);
      queryClient.setQueryData(companySettingsQueryKey, saved);
      toast(t("settings.logoUploaded"), "success");
    } catch (err) {
      toast(err instanceof ApiError ? err.message : t("settings.logoError"), "error");
    } finally {
      setLogoBusy(false);
    }
  };

  const onRemoveLogo = async () => {
    setLogoBusy(true);
    try {
      const saved = await managementRequest<CompanySettingsResponse>(billingApi.companySettingsLogo, {
        method: "DELETE",
      });
      queryClient.setQueryData(companySettingsQueryKey, saved);
      toast(t("settings.logoRemoved"), "success");
    } catch (err) {
      toast(err instanceof ApiError ? err.message : t("settings.logoError"), "error");
    } finally {
      setLogoBusy(false);
    }
  };

  const isNew = data === null && !isLoading && !error;

  return (
    <section className="app-screen space-y-4">
      <PageHeader title={t("settings.title")} subtitle={t("settings.subtitle")} backTo="/profile" />

      {!isLoading && !error ? (
        <div className="settings-tabs">
          {(["general", "invoice", "payment", "email"] as const).map((key) => (
            <button
              key={key}
              className={tab === key ? "settings-tab active" : "settings-tab"}
              onClick={() => setTab(key)}
              type="button"
            >
              {t(`settings.tabs.${key}`)}
            </button>
          ))}
        </div>
      ) : null}

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
            {tab === "general" ? (
              <>
                <FormField
                  label={t("settings.fields.companyName")}
                  error={errors.companyName?.message}
                  {...register("companyName")}
                />
                <FormField
                  label={t("settings.fields.phoneNumber")}
                  type="tel"
                  autoComplete="tel"
                  error={errors.phoneNumber?.message}
                  {...register("phoneNumber")}
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
                  label={t("settings.fields.timeZone")}
                  placeholder={t("settings.fields.timeZonePlaceholder")}
                  error={errors.timeZone?.message}
                  {...register("timeZone")}
                />
                {!isNew ? (
                  <div className="md:col-span-2 space-y-2">
                    <p className="text-sm font-medium">{t("settings.fields.logo")}</p>
                    <p className="text-xs text-secondary">{t("settings.logoHint")}</p>
                    <p className="text-sm text-secondary">
                      {hasLogo ? t("settings.logoPresent") : t("settings.logoMissing")}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <label className="btn-secondary cursor-pointer">
                        <input
                          accept="image/png,image/jpeg,image/webp"
                          className="hidden"
                          disabled={logoBusy}
                          type="file"
                          onChange={(e) => void onLogoSelected(e.target.files?.[0])}
                        />
                        {logoBusy ? t("app.loading") : t("settings.uploadLogo")}
                      </label>
                      {hasLogo ? (
                        <button
                          className="btn-ghost text-sm text-red-500"
                          disabled={logoBusy}
                          type="button"
                          onClick={() => void onRemoveLogo()}
                        >
                          {t("settings.removeLogo")}
                        </button>
                      ) : null}
                    </div>
                  </div>
                ) : null}
              </>
            ) : null}

            {tab === "email" ? (
              <FormField
                label={t("settings.fields.email")}
                type="email"
                autoComplete="email"
                error={errors.email?.message}
                {...register("email")}
              />
            ) : null}

            {tab === "invoice" ? (
              <>
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
                <FormField
                  label={t("settings.fields.brandColor")}
                  placeholder="#FF6B00"
                  error={errors.brandColor?.message}
                  {...register("brandColor")}
                />
                <div className="md:col-span-2">
                  <FormTextArea
                    label={t("settings.fields.invoiceFooterNote")}
                    error={errors.invoiceFooterNote?.message}
                    {...register("invoiceFooterNote")}
                  />
                </div>
              </>
            ) : null}

            {tab === "payment" ? (
              <>
                <p className="md:col-span-2 text-sm text-secondary">{t("settings.paymentTabHint")}</p>
                <label className="md:col-span-2 flex items-center gap-2 text-sm">
                  <input type="checkbox" {...register("enablePaymentReminders")} />
                  {t("settings.fields.enablePaymentReminders")}
                </label>
                <FormField
                  label={t("settings.fields.reminderDaysBeforeDue")}
                  type="number"
                  min={0}
                  max={30}
                  error={errors.reminderDaysBeforeDue?.message}
                  {...register("reminderDaysBeforeDue", { valueAsNumber: true })}
                />
              </>
            ) : null}
          </div>

          {formError ? <p className="text-sm text-red-500">{formError}</p> : null}
          {savedMessage ? <p className="text-sm text-green-600">{savedMessage}</p> : null}

          <button
            className="btn-primary btn-primary--lg w-full"
            disabled={isSubmitting || saveMutation.isPending || (!isNew && !isDirty)}
            type="submit"
          >
            {isSubmitting || saveMutation.isPending ? t("settings.saving") : t("settings.saveChanges")}
          </button>
        </form>
      ) : null}
    </section>
  );
}
