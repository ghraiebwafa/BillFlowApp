import { useTranslation } from "react-i18next";

export function AuthBrandHeader() {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col items-center text-center">
      <img src="/assets/billflow-logo.png" alt="BillFlow" className="mb-3 h-16 w-auto drop-shadow-sm" />
      <p className="text-sm font-medium text-[var(--billflow-maroon)]/80">{t("app.tagline")}</p>
    </div>
  );
}
