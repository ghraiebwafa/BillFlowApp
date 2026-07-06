import { useQuery } from "@tanstack/react-query";
import { History } from "lucide-react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { auditEventSchema } from "../../../domain/billing/schemas";
import {
  auditActionLabel,
  auditEntityLabel,
  type AuditEvent,
} from "../../../domain/billing/audit";

function formatWhen(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

export function ActivityPage() {
  const { t } = useTranslation();

  const { data, isLoading, error } = useQuery({
    queryKey: ["activity"],
    queryFn: () =>
      managementRequest<AuditEvent[]>(billingApi.activity(), {
        schema: z.array(auditEventSchema),
      }),
  });

  const events = data ?? [];

  return (
    <section className="app-screen">
      <PageHeader title={t("activity.title")} subtitle={t("activity.subtitle")} />

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("activity.loadError")}
        </div>
      ) : null}

      <ul className="list-stack">
        {events.map((event) => (
          <li key={event.id} className="card list-row-static">
            <div className="flex items-start gap-3">
              <div className="activity-icon">
                <History className="h-4 w-4 text-accent" aria-hidden />
              </div>
              <div className="min-w-0 flex-1">
                <p className="font-medium">{event.summary}</p>
                <p className="mt-1 text-sm text-secondary">
                  {event.actorDisplayName} · {auditEntityLabel(event.entityType)} ·{" "}
                  {auditActionLabel(event.action)}
                </p>
                <p className="mt-1 text-xs text-secondary">{formatWhen(event.createdAt)}</p>
              </div>
            </div>
          </li>
        ))}
      </ul>

      {!isLoading && !error && events.length === 0 ? (
        <div className="card text-center text-secondary">{t("activity.empty")}</div>
      ) : null}
    </section>
  );
}
