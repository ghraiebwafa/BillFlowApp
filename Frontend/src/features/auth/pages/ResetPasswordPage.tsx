import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import { Lock } from "lucide-react";
import { AuthLayout } from "../../../shared/layout/AuthLayout";
import { FormField } from "../../../shared/ui/FormField";
import { env } from "../../../shared/config/env";
import { requestJson } from "../../../shared/api/http-client";
import { messageResponseSchema } from "../../../domain/auth/schemas";
import { ApiError } from "../../../shared/api/api-error";

const schema = z
  .object({
    newPassword: z.string().min(8),
    confirmNewPassword: z.string().min(8),
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: "Passwords must match",
    path: ["confirmNewPassword"],
  });

type ResetForm = z.infer<typeof schema>;

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetForm>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: "", confirmNewPassword: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    setSuccess(null);
    if (!token) {
      setFormError(t("auth.resetTokenMissing"));
      return;
    }

    try {
      const data = await requestJson<unknown>(
        `${env.authApiUrl}/api/v1.0/auth/account/reset-password`,
        {
          method: "POST",
          body: {
            token,
            newPassword: values.newPassword,
            confirmNewPassword: values.confirmNewPassword,
          },
        },
      );
      const response = messageResponseSchema.parse(data);
      setSuccess(response.message);
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : t("auth.genericError"));
    }
  });

  return (
    <AuthLayout>
      <h2 className="mb-1 text-xl font-semibold">{t("auth.resetTitle")}</h2>
      <p className="mb-6 text-sm text-secondary">{t("auth.resetSubtitle")}</p>

      <form className="space-y-4" onSubmit={onSubmit}>
        <FormField
          label={t("auth.newPassword")}
          type="password"
          showPasswordToggle
          autoComplete="new-password"
          icon={Lock}
          error={errors.newPassword?.message}
          {...register("newPassword")}
        />
        <FormField
          label={t("auth.confirmPassword")}
          type="password"
          showPasswordToggle
          autoComplete="new-password"
          icon={Lock}
          error={errors.confirmNewPassword?.message}
          {...register("confirmNewPassword")}
        />

        {formError ? <p className="text-sm text-red-500">{formError}</p> : null}
        {success ? <p className="text-sm text-green-700">{success}</p> : null}

        <button className="btn-primary w-full" disabled={isSubmitting || Boolean(success)} type="submit">
          {isSubmitting ? t("app.loading") : t("auth.resetSubmit")}
        </button>
      </form>

      <p className="mt-5 text-center text-sm text-secondary">
        <Link to="/login" className="font-medium text-accent no-underline">
          {t("auth.backToLogin")}
        </Link>
      </p>
    </AuthLayout>
  );
}
