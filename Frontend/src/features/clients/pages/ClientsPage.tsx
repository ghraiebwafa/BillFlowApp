import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { MoreVertical, Plus, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { clientInitial, type ClientResponse } from "../../../domain/billing/client";

function useDebouncedValue(value: string, delayMs = 300): string {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

export function ClientsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const { data, isLoading, error } = useQuery({
    queryKey: ["clients", debouncedSearch],
    queryFn: () => {
      const params = debouncedSearch.trim()
        ? `?search=${encodeURIComponent(debouncedSearch.trim())}`
        : "";
      return managementRequest<ClientResponse[]>(`/api/v1.0/billing/Client/GetAll${params}`);
    },
  });

  const clients = data ?? [];

  return (
    <section className="mx-auto max-w-3xl space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-semibold">{t("clients.title")}</h2>
          <p className="text-sm text-secondary">{t("clients.subtitle")}</p>
        </div>
        <button className="btn-primary flex items-center gap-1.5 px-3 py-2 text-sm" type="button">
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">{t("clients.add")}</span>
        </button>
      </div>

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
    </section>
  );
}
