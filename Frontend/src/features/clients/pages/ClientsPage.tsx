import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { PaginationBar } from "../../../shared/ui/PaginationBar";
import { FormField } from "../../../shared/ui/FormField";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { billingApi } from "../../../domain/billing/api-paths";
import { buildPageQuery, pagedSchema, type PagedResponse } from "../../../domain/billing/paging";
import { clientResponseSchema } from "../../../domain/billing/schemas";
import { clientInitial, type ClientResponse } from "../../../domain/billing/client";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";
import { toast } from "../../../shared/ui/toast-store";

const PAGE_SIZE = 50;
const pagedClientSchema = pagedSchema(clientResponseSchema);

type ClientForm = {
  companyName: string;
  contactName: string;
  email: string;
  phoneNumber: string;
  country: string;
  isActive: boolean;
};

const emptyForm: ClientForm = {
  companyName: "",
  contactName: "",
  email: "",
  phoneNumber: "",
  country: "",
  isActive: true,
};

export function ClientsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<ClientResponse | null>(null);
  const [form, setForm] = useState<ClientForm>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);
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

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = {
        companyName: form.companyName.trim(),
        contactName: form.contactName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber.trim() || undefined,
        country: form.country.trim() || undefined,
        ...(editing ? { isActive: form.isActive } : {}),
      };

      if (editing) {
        return managementRequest<ClientResponse>(billingApi.client(editing.id), {
          method: "PUT",
          body,
          schema: clientResponseSchema,
        });
      }

      return managementRequest<ClientResponse>(billingApi.clients, {
        method: "POST",
        body,
        schema: clientResponseSchema,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clients"] });
      setEditorOpen(false);
      setEditing(null);
      setForm(emptyForm);
      setFormError(null);
      toast(t("common.saved"), "success");
    },
    onError: (err) => {
      setFormError(err instanceof ApiError ? err.message : t("clients.saveError"));
    },
  });

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setFormError(null);
    setEditorOpen(true);
  };

  const openEdit = (client: ClientResponse) => {
    setEditing(client);
    setForm({
      companyName: client.companyName,
      contactName: client.contactName,
      email: client.email,
      phoneNumber: client.phoneNumber ?? "",
      country: client.country ?? "",
      isActive: client.isActive,
    });
    setFormError(null);
    setEditorOpen(true);
  };

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
            <li key={client.id}>
              <button className="client-card w-full text-left" type="button" onClick={() => openEdit(client)}>
                <div className="client-avatar" aria-hidden>
                  {clientInitial(client.companyName)}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold">{client.companyName}</p>
                  <p className="truncate text-sm text-secondary">{client.contactName}</p>
                  <p className="truncate text-xs text-secondary">{client.email}</p>
                </div>
                <span className="text-xs text-secondary">
                  {client.isActive ? t("clients.active") : t("clients.inactive")}
                </span>
              </button>
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

      {editorOpen ? (
        <div className="card space-y-3">
          <h3 className="font-semibold">{editing ? t("clients.edit") : t("clients.add")}</h3>
          <FormField
            label={t("clients.companyName")}
            value={form.companyName}
            onChange={(e) => setForm((f) => ({ ...f, companyName: e.target.value }))}
          />
          <FormField
            label={t("clients.contactName")}
            value={form.contactName}
            onChange={(e) => setForm((f) => ({ ...f, contactName: e.target.value }))}
          />
          <FormField
            label={t("clients.email")}
            type="email"
            value={form.email}
            onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
          />
          <FormField
            label={t("clients.phone")}
            value={form.phoneNumber}
            onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value }))}
          />
          <FormField
            label={t("clients.country")}
            value={form.country}
            onChange={(e) => setForm((f) => ({ ...f, country: e.target.value }))}
          />
          {editing ? (
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
              />
              {t("clients.active")}
            </label>
          ) : null}
          {formError ? (
            <p className="text-sm text-red-500" role="alert">
              {formError}
            </p>
          ) : null}
          <div className="flex gap-2">
            <button
              className="btn-secondary flex-1"
              type="button"
              onClick={() => {
                setEditorOpen(false);
                setFormError(null);
              }}
            >
              {t("clients.cancel")}
            </button>
            <button
              className="btn-primary flex-1"
              type="button"
              disabled={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? t("clients.saving") : t("clients.save")}
            </button>
          </div>
        </div>
      ) : null}

      <button className="fab" type="button" aria-label={t("clients.add")} onClick={openCreate}>
        <Plus className="h-6 w-6" />
      </button>
    </section>
  );
}
