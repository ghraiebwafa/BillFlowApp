import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import { Lock, Mail, Phone, User } from "lucide-react";
import { useSessionStore } from "../../../shared/auth/session-store";
import { AuthLayout } from "../../../shared/layout/AuthLayout";
import { FormField } from "../../../shared/ui/FormField";
import { ApiError } from "../../../shared/api/api-error";

const registerSchema = z
  .object({
    fullName: z.string().min(2),
    email: z.string().email(),
    phoneNumber: z.string().optional(),
    password: z.string().min(8),
    confirmPassword: z.string().min(8),
    acceptTerms: z.boolean().refine((value) => value, { message: "You must accept the terms" }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type RegisterForm = z.infer<typeof registerSchema>;

export function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const registerAccount = useSessionStore((s) => s.register);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: "",
      email: "",
      phoneNumber: "",
      password: "",
      confirmPassword: "",
      acceptTerms: false,
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await registerAccount({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        confirmPassword: values.confirmPassword,
        phoneNumber: values.phoneNumber || undefined,
      });
      navigate("/settings/company", { replace: true });
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : t("auth.genericError"));
    }
  });

  return (
    <AuthLayout>
      <h2 className="mb-1 text-xl font-semibold">{t("auth.registerTitle")}</h2>
      <p className="mb-6 text-sm text-secondary">{t("auth.registerSubtitle")}</p>

      <form className="space-y-4" onSubmit={onSubmit}>
        <FormField
          label={t("auth.fullName")}
          autoComplete="name"
          placeholder={t("auth.fullNamePlaceholder")}
          icon={User}
          error={errors.fullName?.message}
          {...register("fullName")}
        />
        <FormField
          label={t("auth.email")}
          type="email"
          autoComplete="email"
          placeholder={t("auth.emailPlaceholder")}
          icon={Mail}
          error={errors.email?.message}
          {...register("email")}
        />
        <FormField
          label={t("auth.phone")}
          type="tel"
          autoComplete="tel"
          placeholder={t("auth.phonePlaceholder")}
          icon={Phone}
          error={errors.phoneNumber?.message}
          {...register("phoneNumber")}
        />
        <FormField
          label={t("auth.password")}
          type="password"
          autoComplete="new-password"
          placeholder={t("auth.passwordPlaceholder")}
          icon={Lock}
          showPasswordToggle
          error={errors.password?.message}
          {...register("password")}
        />
        <FormField
          label={t("auth.confirmPassword")}
          type="password"
          autoComplete="new-password"
          placeholder={t("auth.confirmPasswordPlaceholder")}
          icon={Lock}
          showPasswordToggle
          error={errors.confirmPassword?.message}
          {...register("confirmPassword")}
        />

        <label className="flex items-start gap-2 text-sm text-secondary">
          <input className="mt-1 accent-[var(--billflow-orange)]" type="checkbox" {...register("acceptTerms")} />
          <span>{t("auth.acceptTerms")}</span>
        </label>
        {errors.acceptTerms ? <p className="text-xs text-red-500">{errors.acceptTerms.message}</p> : null}

        {formError ? <p className="text-sm text-red-500">{formError}</p> : null}

        <button className="btn-primary btn-primary--lg w-full" disabled={isSubmitting} type="submit">
          {isSubmitting ? t("auth.creatingAccount") : t("auth.register")}
        </button>
      </form>

      <p className="mt-5 text-center text-sm text-secondary">
        {t("auth.haveAccount")}{" "}
        <Link to="/login" className="font-medium text-accent no-underline">
          {t("auth.login")}
        </Link>
      </p>
    </AuthLayout>
  );
}
