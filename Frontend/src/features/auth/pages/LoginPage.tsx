import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import { Lock, Mail } from "lucide-react";
import { useSessionStore } from "../../../shared/auth/session-store";
import { AuthLayout } from "../../../shared/layout/AuthLayout";
import { FormField } from "../../../shared/ui/FormField";
import { LanguageSwitcher } from "../../../shared/ui/LanguageSwitcher";
import { ThemeToggle } from "../../../shared/ui/ThemeToggle";
import { homePathForRole } from "../../../shared/auth/role-utils";
import { ApiError } from "../../../shared/api/api-error";

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const login = useSessionStore((s) => s.login);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await login(values);
      const user = useSessionStore.getState().user;
      navigate(user ? homePathForRole(user.role) : "/dashboard", { replace: true });
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : t("auth.genericError"));
    }
  });

  return (
    <AuthLayout>
      <div className="mb-4 flex items-center justify-end gap-2">
        <LanguageSwitcher />
        <ThemeToggle />
      </div>

      <h2 className="mb-1 text-xl font-semibold">{t("auth.loginTitle")}</h2>
      <p className="mb-6 text-sm text-secondary">{t("auth.loginSubtitle")}</p>

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
        <FormField
          label={t("auth.password")}
          type="password"
          autoComplete="current-password"
          placeholder={t("auth.passwordPlaceholder")}
          icon={Lock}
          showPasswordToggle
          error={errors.password?.message}
          {...register("password")}
        />

        <div className="text-right">
          <span className="text-sm text-accent">{t("auth.forgotPassword")}</span>
        </div>

        {formError ? <p className="text-sm text-red-500">{formError}</p> : null}

        <button className="btn-primary w-full" disabled={isSubmitting} type="submit">
          {isSubmitting ? t("auth.signingIn") : t("auth.login")}
        </button>
      </form>

      <p className="mt-5 text-center text-sm text-secondary">
        {t("auth.noAccount")}{" "}
        <Link to="/register" className="font-medium text-accent no-underline">
          {t("auth.register")}
        </Link>
      </p>
    </AuthLayout>
  );
}
