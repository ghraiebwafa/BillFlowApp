import { FileSpreadsheet, FileText, PieChart, Receipt } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { billingApi } from "../../../domain/billing/api-paths";
import { downloadWithAuth } from "../../../shared/api/download-with-auth";
import { ApiError } from "../../../shared/api/api-error";
import { toast } from "../../../shared/ui/toast-store";

const reports = [
  { key: "sales", path: billingApi.reports.sales, icon: FileText },
  { key: "payments", path: billingApi.reports.payments, icon: Receipt },
  { key: "outstanding", path: billingApi.reports.outstanding, icon: PieChart },
  { key: "taxes", path: billingApi.reports.taxes, icon: FileSpreadsheet },
] as const;

export function ReportsPage() {
  const { t } = useTranslation();

  const download = async (path: string, filename: string) => {
    try {
      await downloadWithAuth(path, filename);
      toast(t("toast.reportDownloaded"), "success");
    } catch (err) {
      toast(err instanceof ApiError ? err.message : t("reports.downloadError"), "error");
    }
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
