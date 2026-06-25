import { useTranslation } from "react-i18next";

export function AuthBrandHeader() {
  const { t } = useTranslation();

  return (
    <div className="auth-brand">
      <img
        src="/assets/billflow-icon.png"
        alt=""
        className="auth-brand-icon"
        width={72}
        height={72}
      />
      <h1 className="auth-brand-title">BillFlow</h1>
      <p className="auth-brand-tagline">{t("app.tagline")}</p>
    </div>
  );
}
