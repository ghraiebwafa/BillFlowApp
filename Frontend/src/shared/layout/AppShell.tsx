import type { PropsWithChildren } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  FileText,
  LayoutDashboard,
  PieChart,
  Settings,
  Users,
} from "lucide-react";
import { clsx } from "clsx";
import { LanguageSwitcher } from "../ui/LanguageSwitcher";
import { ThemeToggle } from "../ui/ThemeToggle";
import { BottomNav } from "./BottomNav";
import { useSessionStore } from "../auth/session-store";
import { isAdminRole, isVisitorRole } from "../auth/role-utils";
import { roleLabel } from "../auth/route-guards";

const visitorLinks = [
  { to: "/dashboard", labelKey: "nav.dashboard", icon: LayoutDashboard },
  { to: "/clients", labelKey: "nav.clients", icon: Users },
  { to: "/invoices", labelKey: "nav.invoices", icon: FileText },
  { to: "/reports", labelKey: "nav.reports", icon: PieChart },
  { to: "/settings/company", labelKey: "nav.settings", icon: Settings },
] as const;

export function AppShell({ children }: PropsWithChildren) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, logout } = useSessionStore();

  const handleLogout = async () => {
    await logout();
    navigate("/welcome", { replace: true });
  };

  return (
    <div className="min-h-screen bg-surface pb-20 text-primary md:pb-0">
      <aside className="fixed inset-y-0 left-0 hidden w-64 border-r border-muted bg-panel p-4 md:block">
        <img
          src="/assets/billflow-logo.png"
          alt="BillFlow"
          className="mb-6 h-auto w-full max-w-[200px]"
        />
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
      </aside>

      <main className="md:ml-64">
        <header className="sticky top-0 z-10 border-b border-muted bg-panel/95 px-4 py-3 backdrop-blur md:px-6 md:py-4">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-3 md:block">
              <img
                src="/assets/billflow-logo.png"
                alt="BillFlow"
                className="h-8 w-auto md:hidden"
              />
              <div>
                <h1 className="text-lg font-semibold md:text-xl">{t("app.title")}</h1>
                {user ? (
                  <p className="hidden text-sm text-secondary md:block">
                    {user.fullName} · {roleLabel(user.role)}
                  </p>
                ) : null}
              </div>
            </div>
            <div className="flex items-center gap-2">
              <LanguageSwitcher />
              <ThemeToggle />
              <button className="btn-ghost text-sm" onClick={() => void handleLogout()} type="button">
                {t("auth.logout")}
              </button>
            </div>
          </div>
        </header>

        <div className="p-4 md:p-6">{children}</div>
      </main>

      {user && isVisitorRole(user.role) ? <BottomNav /> : null}
    </div>
  );
}
