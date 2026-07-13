import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { PaginationBar } from "../../../shared/ui/PaginationBar";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { buildPageQuery, pagedSchema, type PagedResponse } from "../../../domain/billing/paging";
import { clientResponseSchema } from "../../../domain/billing/schemas";
import { clientInitial, type ClientResponse } from "../../../domain/billing/client";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";

const PAGE_SIZE = 50;
const pagedClientSchema = pagedSchema(clientResponseSchema);

export function ClientsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ["clients", { mode: "list", search: debouncedSearch, page, pageSize: PAGE_SIZE }],
    queryFn: () =>
      managementRequest<PagedResponse<ClientResponse>>(
        `${billingApi.clients}${buildPageQuery({
          search: debouncedSearch,
          page,
          pageSize: PAGE_SIZE,
        })}`,
        { schema: pagedClientSchema },
      ),
    placeholderData: (previous) => previous,
  });

  const clients = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  return (
    <section className="app-screen space-y-4">
      <PageHeader title={t("clients.title")} subtitle={t("clients.subtitle")} />

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("clients.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label={t("clients.searchPlaceholder")}
        />
      </label>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}

      {error ? (
        <div className="card text-red-500" role="alert">
          {error instanceof ApiError ? error.message : t("clients.loadError")}
        </div>
      ) : null}

      {!isLoading && !error && clients.length === 0 ? (
        <div className="card text-center text-secondary">{t("clients.empty")}</div>
      ) : null}

      {!isLoading && !error && clients.length > 0 ? (
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
            </li>
          ))}
        </ul>
      ) : null}

      <PaginationBar
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
        disabled={isFetching}
      />
    </section>
  );
}
