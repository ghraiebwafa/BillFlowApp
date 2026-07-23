import type { PropsWithChildren } from "react";
import { Link, NavLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  CreditCard,
  FileText,
  LayoutDashboard,
  PieChart,
  Settings,
  User,
  Users,
} from "lucide-react";
import { clsx } from "clsx";
import { BottomNav } from "./BottomNav";
import { BillFlowLogo } from "../ui/BillFlowLogo";
import { AppPreferences } from "../ui/AppPreferences";
import { useSessionStore } from "../auth/session-store";
import { isAdminRole, isVisitorRole } from "../auth/role-utils";

const visitorLinks = [
  { to: "/dashboard", labelKey: "nav.dashboard", icon: LayoutDashboard },
  { to: "/invoices", labelKey: "nav.invoices", icon: FileText },
  { to: "/clients", labelKey: "nav.clients", icon: Users },
  { to: "/payments", labelKey: "nav.payments", icon: CreditCard },
  { to: "/reports", labelKey: "nav.reports", icon: PieChart },
  { to: "/profile", labelKey: "nav.profile", icon: User },
  { to: "/settings/company", labelKey: "nav.settings", icon: Settings },
] as const;

export function AppShell({ children }: PropsWithChildren) {
  const { t } = useTranslation();
  const { user } = useSessionStore();

  return (
    <div className="min-h-screen bg-surface pb-20 text-primary md:pb-0">
      <aside className="app-sidebar fixed inset-y-0 left-0 hidden w-64 border-r border-muted bg-panel p-5 md:flex md:flex-col">
        <div className="mb-7">
          <BillFlowLogo size="header" />
          <p className="mt-2 text-[0.65rem] font-semibold uppercase tracking-[0.14em] text-secondary">
            {t("app.kicker")}
          </p>
        </div>
        <nav className="flex flex-col gap-1 text-sm">
          {user && isVisitorRole(user.role)
            ? visitorLinks.map(({ to, labelKey, icon: Icon }) => (
                <NavLink
                  key={to}
                  to={to}
                  className={({ isActive }) => clsx("nav-link flex items-center gap-2", isActive && "active")}
                >
                  <Icon className="h-4 w-4" strokeWidth={1.75} />
                  {t(labelKey)}
                </NavLink>
              ))
            : null}
          {user && isAdminRole(user.role) ? (
            <Link to="/admin/users" className="nav-link">
              {t("nav.admin")}
            </Link>
          ) : null}
        </nav>

        <div className="sidebar-preferences mt-auto border-t border-muted pt-4">
          <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-secondary">
            {t("profile.appearance")}
          </p>
          <AppPreferences />
        </div>
      </aside>

      <main className="md:ml-64">
        <div className="p-4 md:p-6">{children}</div>
      </main>

      {user && isVisitorRole(user.role) ? <BottomNav /> : null}
    </div>
  );
}
