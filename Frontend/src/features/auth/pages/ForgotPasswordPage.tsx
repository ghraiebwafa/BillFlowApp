import { useState } from "react";
import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import { Mail } from "lucide-react";
import { AuthLayout } from "../../../shared/layout/AuthLayout";
import { FormField } from "../../../shared/ui/FormField";
import { env } from "../../../shared/config/env";
import { requestJson } from "../../../shared/api/http-client";
import { messageResponseSchema } from "../../../domain/auth/schemas";
import { ApiError } from "../../../shared/api/api-error";

const schema = z.object({
  email: z.string().email(),
});

type ForgotForm = z.infer<typeof schema>;

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotForm>({
    resolver: zodResolver(schema),
    defaultValues: { email: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    setSuccess(null);

    try {
      const data = await requestJson<unknown>(
        `${env.authApiUrl}/api/v1.0/auth/account/forgot-password`,
        { method: "POST", body: values },
      );
      const response = messageResponseSchema.parse(data);
      setSuccess(response.message);
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : t("auth.genericError"));
    }
  });

  return (
    <AuthLayout>
      <h2 className="mb-1 text-xl font-semibold">{t("auth.forgotTitle")}</h2>
      <p className="mb-6 text-sm text-secondary">{t("auth.forgotSubtitle")}</p>

      <form className="space-y-4" onSubmit={onSubmit}>
        <FormField
          label={t("auth.email")}
          type="email"
          autoComplete="email"
          placeholder={t("auth.emailPlaceholder")}
          icon={Mail}
          error={errors.email?.message}
          {...register("email")}
        />

        {formError ? <p className="text-sm text-red-500">{formError}</p> : null}
        {success ? <p className="text-sm text-green-700">{success}</p> : null}

        <button className="btn-primary w-full" disabled={isSubmitting} type="submit">
          {isSubmitting ? t("auth.sendingReset") : t("auth.sendResetLink")}
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
