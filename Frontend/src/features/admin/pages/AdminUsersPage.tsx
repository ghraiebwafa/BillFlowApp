import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { FormField } from "../../../shared/ui/FormField";
import { managementRequest } from "../../../shared/api/management-client";
import { ApiError } from "../../../shared/api/api-error";
import { messageResponseSchema } from "../../../domain/auth/schemas";
import { UserRole } from "../../../domain/auth/types";
import { managementApi } from "../../../domain/management/api-paths";
import {
  userManagementListSchema,
  userManagementResponseSchema,
  type UserManagementResponse,
} from "../../../domain/management/schemas";
import { useSessionStore } from "../../../shared/auth/session-store";
import { toast } from "../../../shared/ui/toast-store";
import { normalizeRole } from "../../../shared/auth/role-utils";

type TabKey = "visitors" | "admins";

type EditorForm = {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
  isActive: boolean;
};

const emptyForm: EditorForm = {
  fullName: "",
  email: "",
  password: "",
  phoneNumber: "",
  isActive: true,
};

function roleLabel(role: UserRole, t: (key: string) => string): string {
  if (role === UserRole.SuperAdmin) return t("admin.roles.superAdmin");
  if (role === UserRole.Admin) return t("admin.roles.admin");
  return t("admin.roles.visitor");
}

export function AdminUsersPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const user = useSessionStore((s) => s.user);
  const isSuperAdmin = user?.role === UserRole.SuperAdmin;

  const [tab, setTab] = useState<TabKey>("visitors");
  const [search, setSearch] = useState("");
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<UserManagementResponse | null>(null);
  const [form, setForm] = useState<EditorForm>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!isSuperAdmin && tab === "admins") {
      setTab("visitors");
    }
  }, [isSuperAdmin, tab]);

  const visitorsQuery = useQuery({
    queryKey: ["management-visitors"],
    queryFn: () =>
      managementRequest<UserManagementResponse[]>(managementApi.visitors, {
        schema: userManagementListSchema,
      }),
  });

  const adminsQuery = useQuery({
    queryKey: ["management-admins"],
    enabled: isSuperAdmin,
    queryFn: () =>
      managementRequest<UserManagementResponse[]>(managementApi.admins, {
        schema: userManagementListSchema,
      }),
  });

  const activeQuery = tab === "admins" ? adminsQuery : visitorsQuery;
  const rows = useMemo(() => {
    const list = (activeQuery.data ?? []).map((row) => ({
      ...row,
      role: normalizeRole(row.role),
    }));
    const q = search.trim().toLowerCase();
    if (!q) return list;
    return list.filter(
      (row) =>
        row.fullName.toLowerCase().includes(q)
        || row.email.toLowerCase().includes(q)
        || (row.phoneNumber ?? "").toLowerCase().includes(q),
    );
  }, [activeQuery.data, search]);

  const closeEditor = () => {
    setEditorOpen(false);
    setEditing(null);
    setForm(emptyForm);
    setFormError(null);
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      const fullName = form.fullName.trim();
      const phoneNumber = form.phoneNumber.trim() || undefined;

      if (tab === "admins") {
        if (editing) {
          return managementRequest<UserManagementResponse>(managementApi.admin(editing.id), {
            method: "PUT",
            body: { fullName, phoneNumber, isActive: form.isActive },
            schema: userManagementResponseSchema,
          });
        }
        return managementRequest<UserManagementResponse>(managementApi.admins, {
          method: "POST",
          body: {
            fullName,
            email: form.email.trim(),
            password: form.password,
            phoneNumber,
          },
          schema: userManagementResponseSchema,
        });
      }

      if (!editing) {
        throw new ApiError(t("admin.visitorsCreateHint"), 400);
      }

      return managementRequest<UserManagementResponse>(managementApi.visitor(editing.id), {
        method: "PUT",
        body: { fullName, phoneNumber, isActive: form.isActive },
        schema: userManagementResponseSchema,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: tab === "admins" ? ["management-admins"] : ["management-visitors"],
      });
      closeEditor();
      toast(t("common.saved"), "success");
    },
    onError: (err) => {
      setFormError(err instanceof ApiError ? err.message : t("admin.saveError"));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) =>
      managementRequest<{ message: string }>(
        tab === "admins" ? managementApi.admin(id) : managementApi.visitor(id),
        {
          method: "DELETE",
          schema: messageResponseSchema,
        },
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: tab === "admins" ? ["management-admins"] : ["management-visitors"],
      });
      closeEditor();
      toast(t("toast.userDeactivated"), "success");
    },
    onError: (err) => {
      toast(err instanceof ApiError ? err.message : t("admin.saveError"), "error");
    },
  });

  const openCreateAdmin = () => {
    setEditing(null);
    setForm(emptyForm);
    setFormError(null);
    setEditorOpen(true);
  };

  const openEdit = (row: UserManagementResponse) => {
    setEditing(row);
    setForm({
      fullName: row.fullName,
      email: row.email,
      password: "",
      phoneNumber: row.phoneNumber ?? "",
      isActive: row.isActive,
    });
    setFormError(null);
    setEditorOpen(true);
  };

  const busy = saveMutation.isPending || deleteMutation.isPending;

  return (
    <section className="app-screen space-y-4">
      <PageHeader title={t("admin.title")} subtitle={t("admin.subtitle")} />

      <div className="step-tabs" style={{ gridTemplateColumns: isSuperAdmin ? "1fr 1fr" : "1fr" }}>
        <button
          className={tab === "visitors" ? "step-tab active" : "step-tab"}
          type="button"
          onClick={() => {
            setTab("visitors");
            closeEditor();
            setSearch("");
          }}
        >
          {t("admin.tabs.visitors")}
        </button>
        {isSuperAdmin ? (
          <button
            className={tab === "admins" ? "step-tab active" : "step-tab"}
            type="button"
            onClick={() => {
              setTab("admins");
              closeEditor();
              setSearch("");
            }}
          >
            {t("admin.tabs.admins")}
          </button>
        ) : null}
      </div>

      {!isSuperAdmin ? (
        <p className="text-sm text-secondary">{t("admin.adminOnlyHint")}</p>
      ) : null}

      <label className="search-input-wrap block">
        <Search className="h-4 w-4 shrink-0 text-secondary" aria-hidden />
        <input
          placeholder={t("admin.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label={t("admin.searchPlaceholder")}
        />
      </label>

      {activeQuery.isLoading ? <div className="card">{t("app.loading")}</div> : null}
      {activeQuery.error ? (
        <div className="card text-red-500" role="alert">
          {activeQuery.error instanceof ApiError
            ? activeQuery.error.message
            : t("admin.loadError")}
        </div>
      ) : null}

      {!activeQuery.isLoading && !activeQuery.error && rows.length === 0 ? (
        <div className="card text-center text-secondary">
          {tab === "admins" ? t("admin.emptyAdmins") : t("admin.emptyVisitors")}
        </div>
      ) : null}

      {!activeQuery.isLoading && !activeQuery.error && rows.length > 0 ? (
        <ul className="list-stack">
          {rows.map((row) => (
            <li key={row.id}>
              <button className="card list-row-static w-full text-left" type="button" onClick={() => openEdit(row)}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="font-semibold">{row.fullName}</p>
                    <p className="truncate text-sm text-secondary">{row.email}</p>
                    <p className="mt-1 text-xs text-secondary">
                      {roleLabel(row.role, t)} · {row.isActive ? t("admin.active") : t("admin.inactive")}
                    </p>
                  </div>
                </div>
              </button>
            </li>
          ))}
        </ul>
      ) : null}

      {editorOpen ? (
        <div className="card space-y-3">
          <h3 className="font-semibold">
            {editing
              ? tab === "admins"
                ? t("admin.editAdmin")
                : t("admin.editVisitor")
              : t("admin.addAdmin")}
          </h3>
          <FormField
            label={t("admin.fields.fullName")}
            value={form.fullName}
            onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))}
          />
          {!editing && tab === "admins" ? (
            <>
              <FormField
                label={t("admin.fields.email")}
                type="email"
                value={form.email}
                onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
              />
              <FormField
                label={t("admin.fields.password")}
                type="password"
                showPasswordToggle
                value={form.password}
                onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                autoComplete="new-password"
              />
            </>
          ) : null}
          {editing ? (
            <FormField label={t("admin.fields.email")} value={form.email} disabled />
          ) : null}
          <FormField
            label={t("admin.fields.phone")}
            value={form.phoneNumber}
            onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value }))}
          />
          {editing ? (
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
              />
              {t("admin.active")}
            </label>
          ) : null}
          {formError ? (
            <p className="text-sm text-red-500" role="alert">
              {formError}
            </p>
          ) : null}
          <div className="flex gap-2">
            <button className="btn-secondary flex-1" type="button" onClick={closeEditor}>
              {t("admin.cancel")}
            </button>
            <button
              className="btn-primary flex-1"
              type="button"
              disabled={busy}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? t("admin.saving") : t("admin.save")}
            </button>
          </div>
          {editing ? (
            <button
              className="btn-ghost w-full text-sm text-red-500"
              type="button"
              disabled={busy}
              onClick={() => {
                if (window.confirm(t("admin.deleteConfirm"))) {
                  deleteMutation.mutate(editing.id);
                }
              }}
            >
              {t("admin.deactivate")}
            </button>
          ) : null}
        </div>
      ) : null}

      {isSuperAdmin && tab === "admins" ? (
        <button className="fab" type="button" aria-label={t("admin.addAdmin")} onClick={openCreateAdmin}>
          <Plus className="h-6 w-6" />
        </button>
      ) : null}
    </section>
  );
}
