import { FileSpreadsheet, FileText, PieChart, Receipt } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { env } from "../../../shared/config/env";
import { useSessionStore } from "../../../shared/auth/session-store";

const reports = [
  { key: "sales", path: "/api/v1.0/billing/Reports/ExportSales", icon: FileText },
  { key: "payments", path: "/api/v1.0/billing/Reports/ExportPayments", icon: Receipt },
  { key: "outstanding", path: "/api/v1.0/billing/Reports/ExportOutstanding", icon: PieChart },
  { key: "taxes", path: "/api/v1.0/billing/Reports/ExportTaxes", icon: FileSpreadsheet },
] as const;

export function ReportsPage() {
  const { t } = useTranslation();
  const accessToken = useSessionStore((s) => s.accessToken);

  const download = async (path: string, filename: string) => {
    if (!accessToken) return;
    const response = await fetch(`${env.managementApiUrl}${path}`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (!response.ok) return;
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <section className="app-screen">
      <PageHeader title={t("nav.reports")} subtitle={t("reports.subtitle")} />

      <ul className="list-stack">
        {reports.map(({ key, path, icon: Icon }) => (
          <li key={key}>
            <button
              className="report-row"
              onClick={() => void download(path, `billflow-${key}.csv`)}
              type="button"
            >
              <div className="report-row-icon">
                <Icon className="h-5 w-5 text-accent" strokeWidth={1.75} />
              </div>
              <div className="min-w-0 flex-1 text-left">
                <p className="font-semibold">{t(`reports.items.${key}.title`)}</p>
                <p className="text-sm text-secondary">{t(`reports.items.${key}.desc`)}</p>
              </div>
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
