import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { MoreVertical, Plus, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { buildPageQuery, type PagedResponse } from "../../../domain/billing/paging";
import { clientResponseSchema } from "../../../domain/billing/schemas";
import { clientInitial, type ClientResponse } from "../../../domain/billing/client";

function useDebouncedValue(value: string, delayMs = 300): string {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

const pagedClientSchema = z.object({
  items: z.array(clientResponseSchema),
  totalCount: z.number().int(),
  page: z.number().int(),
  pageSize: z.number().int(),
});

export function ClientsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const { data, isLoading, error } = useQuery({
    queryKey: ["clients", debouncedSearch],
    queryFn: () =>
      managementRequest<PagedResponse<ClientResponse>>(
        `${billingApi.clients}${buildPageQuery({
          search: debouncedSearch,
          page: 1,
          pageSize: 50,
        })}`,
        { schema: pagedClientSchema },
      ),
  });

  const clients = data?.items ?? [];

  return (
    <section className="app-screen space-y-4">
      <PageHeader title={t("clients.title")} subtitle={t("clients.subtitle")} />

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("clients.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </label>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}

      {error ? (
        <div className="card text-red-500">
          {error instanceof ApiError ? error.message : t("clients.loadError")}
        </div>
      ) : null}

      {!isLoading && !error && clients.length === 0 ? (
        <div className="card text-center text-secondary">{t("clients.empty")}</div>
      ) : null}

      <ul className="space-y-3">
        {clients.map((client) => (
          <li key={client.id} className="client-card">
            <div className="client-avatar" aria-hidden>
              {clientInitial(client.companyName)}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold">{client.companyName}</p>
              <p className="truncate text-sm text-secondary">{client.contactName}</p>
              <p className="truncate text-xs text-secondary">{client.email}</p>
            </div>
            <button
              className="btn-ghost shrink-0 p-1 text-secondary"
              type="button"
              aria-label={t("clients.actions")}
            >
              <MoreVertical className="h-5 w-5" />
            </button>
          </li>
        ))}
      </ul>

      <button className="fab md:hidden" type="button" aria-label={t("clients.add")}>
        <Plus className="h-6 w-6" />
      </button>
    </section>
  );
}
