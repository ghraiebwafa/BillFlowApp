import { NavLink } from "react-router-dom";
import { CreditCard, FileText, Home, PieChart, User, Users } from "lucide-react";
import { useTranslation } from "react-i18next";
import { clsx } from "clsx";

const items = [
  { to: "/dashboard", labelKey: "nav.home", icon: Home },
  { to: "/invoices", labelKey: "nav.invoices", icon: FileText },
  { to: "/clients", labelKey: "nav.clients", icon: Users },
  { to: "/payments", labelKey: "nav.payments", icon: CreditCard },
  { to: "/reports", labelKey: "nav.reports", icon: PieChart },
  { to: "/profile", labelKey: "nav.profile", icon: User },
] as const;

export function BottomNav() {
  const { t } = useTranslation();

  return (
    <nav className="bottom-nav fixed inset-x-0 bottom-0 z-20 md:hidden" aria-label="Main">
      <div className="mx-auto grid max-w-lg grid-cols-6 gap-0.5">
        {items.map(({ to, labelKey, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              clsx("bottom-nav-link rounded-lg py-1", isActive && "active")
            }
          >
            <Icon className="h-5 w-5" strokeWidth={1.75} />
            <span>{t(labelKey)}</span>
          </NavLink>
        ))}
      </div>
    </nav>
  );
}
