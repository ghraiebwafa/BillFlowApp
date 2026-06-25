import { FileSpreadsheet, FileText, PieChart, Receipt } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { downloadWithAuth } from "../../../shared/api/download-with-auth";
import { ApiError } from "../../../shared/api/api-error";
import { useState } from "react";

const reports = [
  { key: "sales", path: "/api/v1.0/billing/Reports/ExportSales", icon: FileText },
  { key: "payments", path: "/api/v1.0/billing/Reports/ExportPayments", icon: Receipt },
  { key: "outstanding", path: "/api/v1.0/billing/Reports/ExportOutstanding", icon: PieChart },
  { key: "taxes", path: "/api/v1.0/billing/Reports/ExportTaxes", icon: FileSpreadsheet },
] as const;

export function ReportsPage() {
  const { t } = useTranslation();
  const [error, setError] = useState<string | null>(null);

  const download = async (path: string, filename: string) => {
    setError(null);
    try {
      await downloadWithAuth(path, filename);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t("reports.downloadError"));
    }
  };

  return (
    <section className="app-screen">
      <PageHeader title={t("nav.reports")} subtitle={t("reports.subtitle")} />

      {error ? <div className="card text-red-500">{error}</div> : null}

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
