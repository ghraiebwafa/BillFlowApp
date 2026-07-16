import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AuthLayout } from "../../../shared/layout/AuthLayout";
import { env } from "../../../shared/config/env";
import { requestJson } from "../../../shared/api/http-client";
import { messageResponseSchema } from "../../../domain/auth/schemas";
import { ApiError } from "../../../shared/api/api-error";

export function VerifyEmailPage() {
  const { t } = useTranslation();
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      if (!token) {
        setError(t("auth.verifyTokenMissing"));
        setLoading(false);
        return;
      }

      try {
        const data = await requestJson<unknown>(
          `${env.authApiUrl}/api/v1.0/auth/account/confirm-email`,
          { method: "POST", body: { token } },
        );
        if (cancelled) return;
        setMessage(messageResponseSchema.parse(data).message);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : t("auth.genericError"));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void run();
    return () => {
      cancelled = true;
    };
  }, [t, token]);

  return (
    <AuthLayout>
      <h2 className="mb-1 text-xl font-semibold">{t("auth.verifyTitle")}</h2>
      <p className="mb-6 text-sm text-secondary">{t("auth.verifySubtitle")}</p>
      {loading ? <p className="text-sm text-secondary">{t("app.loading")}</p> : null}
      {error ? <p className="text-sm text-red-500">{error}</p> : null}
      {message ? <p className="text-sm text-green-700">{message}</p> : null}
      <p className="mt-5 text-center text-sm text-secondary">
        <Link to="/login" className="font-medium text-accent no-underline">
          {t("auth.backToLogin")}
        </Link>
      </p>
    </AuthLayout>
  );
}
