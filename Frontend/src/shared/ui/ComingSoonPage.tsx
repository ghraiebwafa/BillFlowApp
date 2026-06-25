import { useTranslation } from "react-i18next";
import { PageHeader } from "./PageHeader";

type ComingSoonPageProps = {
  titleKey: string;
  subtitleKey?: string;
};

export function ComingSoonPage({ titleKey, subtitleKey }: ComingSoonPageProps) {
  const { t } = useTranslation();

  return (
    <section className="mx-auto max-w-2xl space-y-3">
      <PageHeader title={t(titleKey)} subtitle={subtitleKey ? t(subtitleKey) : undefined} />
      <div className="card text-center text-secondary" role="status">
        {t("common.comingSoonBody")}
      </div>
    </section>
  );
}
