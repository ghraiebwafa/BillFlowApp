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

const PAGE_SIZE = 50;
const pagedItemSchema = pagedSchema(itemResponseSchema);

type ItemForm = {
  name: string;
  description: string;
  unitPrice: number;
  currency: string;
  vatRate: number;
};

export function ItemsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const companyCurrency = useCompanyCurrency();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editorOpen, setEditorOpen] = useState(false);
  const [form, setForm] = useState<ItemForm>({
    name: "",
    description: "",
    unitPrice: 0,
    currency: "USD",
    vatRate: 0,
  });
  const [formError, setFormError] = useState<string | null>(null);
  const debouncedSearch = useDebouncedValue(search);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  useEffect(() => {
    if (!editorOpen) {
      setForm((f) => ({ ...f, currency: companyCurrency }));
    }
  }, [companyCurrency, editorOpen]);

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ["items", { search: debouncedSearch, page, pageSize: PAGE_SIZE }],
    queryFn: () => {
      return managementRequest<PagedResponse<ItemResponse>>(
        `${billingApi.items}${buildPageQuery({
          search: debouncedSearch,
          page,
          pageSize: PAGE_SIZE,
        })}`,
        { schema: pagedItemSchema },
      );
    },
    placeholderData: (previous) => previous,
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      managementRequest<ItemResponse>(billingApi.items, {
        method: "POST",
        body: {
          name: form.name.trim(),
          description: form.description.trim() || undefined,
          unitPrice: form.unitPrice,
          currency: form.currency.trim().toUpperCase(),
          vatRate: form.vatRate,
        },
        schema: itemResponseSchema,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["items"] });
      setEditorOpen(false);
      setForm({
        name: "",
        description: "",
        unitPrice: 0,
        currency: companyCurrency,
        vatRate: 0,
      });
      setFormError(null);
      toast(t("common.saved"), "success");
    },
    onError: (err) => {
      setFormError(err instanceof ApiError ? err.message : t("items.saveError"));
    },
  });

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

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
            <li key={item.id} className="card list-row-static">
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <p className="font-semibold">{item.name}</p>
                  {item.description ? (
                    <p className="text-sm text-secondary truncate">{item.description}</p>
                  ) : null}
                </div>
                <p className="font-semibold text-accent">
                  {formatMoney(item.unitPrice, item.currency || companyCurrency)}
                </p>
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

      {editorOpen ? (
        <div className="card space-y-3">
          <h3 className="font-semibold">{t("items.add")}</h3>
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
          {formError ? (
            <p className="text-sm text-red-500" role="alert">
              {formError}
            </p>
          ) : null}
          <div className="flex gap-2">
            <button className="btn-secondary flex-1" type="button" onClick={() => setEditorOpen(false)}>
              {t("items.cancel")}
            </button>
            <button
              className="btn-primary flex-1"
              type="button"
              disabled={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? t("items.saving") : t("items.save")}
            </button>
          </div>
        </div>
      ) : null}

      <button
        className="fab"
        type="button"
        aria-label={t("items.add")}
        onClick={() => {
          setFormError(null);
          setForm({
            name: "",
            description: "",
            unitPrice: 0,
            currency: companyCurrency,
            vatRate: 0,
          });
          setEditorOpen(true);
        }}
      >
        <Plus className="h-6 w-6" />
      </button>
    </section>
  );
}
