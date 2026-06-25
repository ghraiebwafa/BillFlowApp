import { useTranslation } from "react-i18next";

export function AdminUsersPage() {
  const { t } = useTranslation();
  return (
    <section className="space-y-3">
      <h2 className="text-2xl font-semibold">{t("admin.title")}</h2>
      <p className="text-secondary">{t("admin.subtitle")}</p>
      <div className="card">{t("admin.placeholder")}</div>
    </section>
  );
}
