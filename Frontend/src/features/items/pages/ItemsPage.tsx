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
import { itemResponseSchema } from "../../../domain/billing/schemas";
import type { ItemResponse } from "../../../domain/billing/item";
import { formatMoney, useCompanyCurrency } from "../../../shared/lib/money";
import { useDebouncedValue } from "../../../shared/lib/use-debounced-value";
import { toast } from "../../../shared/ui/toast-store";
import { z } from "zod";

const PAGE_SIZE = 50;
const pagedItemSchema = pagedSchema(itemResponseSchema);
const messageSchema = z.object({ message: z.string() });

type ItemForm = {
  name: string;
  description: string;
  unitPrice: number;
  currency: string;
  vatRate: number;
  isActive: boolean;
};

const emptyForm = (currency: string): ItemForm => ({
  name: "",
  description: "",
  unitPrice: 0,
  currency,
  vatRate: 0,
  isActive: true,
});

export function ItemsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const companyCurrency = useCompanyCurrency();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<ItemResponse | null>(null);
  const [form, setForm] = useState<ItemForm>(() => emptyForm(companyCurrency));
  const [formError, setFormError] = useState<string | null>(null);
  const debouncedSearch = useDebouncedValue(search);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  useEffect(() => {
    if (!editorOpen && !editing) {
      setForm((f) => ({ ...f, currency: companyCurrency }));
    }
  }, [companyCurrency, editorOpen, editing]);

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ["items", { search: debouncedSearch, page, pageSize: PAGE_SIZE }],
    queryFn: () =>
      managementRequest<PagedResponse<ItemResponse>>(
        `${billingApi.items}${buildPageQuery({
          search: debouncedSearch,
          page,
          pageSize: PAGE_SIZE,
        })}`,
        { schema: pagedItemSchema },
      ),
    placeholderData: (previous) => previous,
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = {
        name: form.name.trim(),
        description: form.description.trim() || undefined,
        unitPrice: form.unitPrice,
        currency: form.currency.trim().toUpperCase(),
        vatRate: form.vatRate,
        ...(editing ? { isActive: form.isActive } : {}),
      };

      if (editing) {
        return managementRequest<ItemResponse>(billingApi.item(editing.id), {
          method: "PUT",
          body,
          schema: itemResponseSchema,
        });
      }

      return managementRequest<ItemResponse>(billingApi.items, {
        method: "POST",
        body,
        schema: itemResponseSchema,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["items"] });
      setEditorOpen(false);
      setEditing(null);
      setForm(emptyForm(companyCurrency));
      setFormError(null);
      toast(t("common.saved"), "success");
    },
    onError: (err) => {
      setFormError(err instanceof ApiError ? err.message : t("items.saveError"));
    },
  });

  const archiveMutation = useMutation({
    mutationFn: (id: string) =>
      managementRequest<ItemResponse>(billingApi.itemArchive(id), {
        method: "POST",
        schema: itemResponseSchema,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["items"] });
      setEditorOpen(false);
      setEditing(null);
      toast(t("toast.itemArchived"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("items.saveError"), "error");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) =>
      managementRequest<{ message: string }>(billingApi.item(id), {
        method: "DELETE",
        schema: messageSchema,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["items"] });
      setEditorOpen(false);
      setEditing(null);
      toast(t("toast.itemDeleted"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("items.saveError"), "error");
    },
  });

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm(companyCurrency));
    setFormError(null);
    setEditorOpen(true);
  };

  const openEdit = (item: ItemResponse) => {
    setEditing(item);
    setForm({
      name: item.name,
      description: item.description ?? "",
      unitPrice: item.unitPrice,
      currency: item.currency || companyCurrency,
      vatRate: item.vatRate,
      isActive: item.isActive,
    });
    setFormError(null);
    setEditorOpen(true);
  };

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const busy = saveMutation.isPending || archiveMutation.isPending || deleteMutation.isPending;

  return (
    <section className="app-screen space-y-4">
      <PageHeader title={t("items.title")} subtitle={t("items.subtitle")} />

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("items.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label={t("items.searchPlaceholder")}
        />
      </label>

      {isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {error ? (
        <div className="card text-red-500" role="alert">
          {error instanceof ApiError ? error.message : t("items.loadError")}
        </div>
      ) : null}

      {!isLoading && !error && items.length === 0 ? (
        <div className="card text-center text-secondary">{t("items.empty")}</div>
      ) : null}

      {!isLoading && !error && items.length > 0 ? (
        <ul className="list-stack">
          {items.map((item) => (
            <li key={item.id}>
              <button className="card list-row-static w-full text-left" type="button" onClick={() => openEdit(item)}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="font-semibold">{item.name}</p>
                    {item.description ? (
                      <p className="truncate text-sm text-secondary">{item.description}</p>
                    ) : null}
                    <p className="mt-1 text-xs text-secondary">
                      {item.isArchived
                        ? t("items.archived")
                        : item.isActive
                          ? t("items.active")
                          : t("items.inactive")}
                    </p>
                  </div>
                  <p className="font-semibold text-accent">
                    {formatMoney(item.unitPrice, item.currency || companyCurrency)}
                  </p>
                </div>
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
          <h3 className="font-semibold">{editing ? t("items.edit") : t("items.add")}</h3>
          <FormField
            label={t("items.name")}
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          />
          <FormField
            label={t("items.description")}
            value={form.description}
            onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
          />
          <FormField
            label={t("items.unitPrice")}
            type="number"
            step="0.01"
            min={0}
            value={form.unitPrice}
            onChange={(e) => setForm((f) => ({ ...f, unitPrice: Number(e.target.value) }))}
          />
          <FormField
            label={t("items.currency")}
            value={form.currency}
            onChange={(e) => setForm((f) => ({ ...f, currency: e.target.value }))}
          />
          <FormField
            label={t("items.vatRate")}
            type="number"
            step="0.01"
            min={0}
            value={form.vatRate}
            onChange={(e) => setForm((f) => ({ ...f, vatRate: Number(e.target.value) }))}
          />
          {editing ? (
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
              />
              {t("items.active")}
            </label>
          ) : null}
          {formError ? (
            <p className="text-sm text-red-500" role="alert">
              {formError}
            </p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <button
              className="btn-secondary flex-1"
              type="button"
              onClick={() => {
                setEditorOpen(false);
                setEditing(null);
                setFormError(null);
              }}
            >
              {t("items.cancel")}
            </button>
            <button
              className="btn-primary flex-1"
              type="button"
              disabled={busy}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? t("items.saving") : t("items.save")}
            </button>
          </div>
          {editing && !editing.isArchived ? (
            <button
              className="btn-ghost w-full text-sm"
              type="button"
              disabled={busy}
              onClick={() => {
                if (window.confirm(t("items.archiveConfirm"))) {
                  archiveMutation.mutate(editing.id);
                }
              }}
            >
              {t("items.archive")}
            </button>
          ) : null}
          {editing ? (
            <button
              className="btn-ghost w-full text-sm text-red-500"
              type="button"
              disabled={busy}
              onClick={() => {
                if (window.confirm(t("items.deleteConfirm"))) {
                  deleteMutation.mutate(editing.id);
                }
              }}
            >
              {t("items.delete")}
            </button>
          ) : null}
        </div>
      ) : null}

      <button className="fab" type="button" aria-label={t("items.add")} onClick={openCreate}>
        <Plus className="h-6 w-6" />
      </button>
    </section>
  );
}
