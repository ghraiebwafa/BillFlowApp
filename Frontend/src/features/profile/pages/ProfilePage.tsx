import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Bell,
  Building2,
  ChevronRight,
  Globe,
  History,
  KeyRound,
  LogOut,
  SlidersHorizontal,
  User,
} from "lucide-react";
import { PageHeader } from "../../../shared/ui/PageHeader";
import { LanguageSwitcher } from "../../../shared/ui/LanguageSwitcher";
import { ThemeToggle } from "../../../shared/ui/ThemeToggle";
import { useSessionStore } from "../../../shared/auth/session-store";
import { toast } from "../../../shared/ui/toast-store";

function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

export function ProfilePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, logout } = useSessionStore();

  if (!user) return null;

  const menuItems: Array<{
    to: string | null;
    labelKey: string;
    icon: typeof Building2;
    disabled?: boolean;
  }> = [
    { to: "/settings/company", labelKey: "profile.companySettings", icon: Building2 },
    { to: "/activity", labelKey: "profile.activity", icon: History },
    { to: null, labelKey: "profile.myProfile", icon: User, disabled: true },
    { to: null, labelKey: "profile.changePassword", icon: KeyRound, disabled: true },
    { to: null, labelKey: "profile.preferences", icon: SlidersHorizontal, disabled: true },
    { to: null, labelKey: "profile.notifications", icon: Bell, disabled: true },
  ];

  const handleLogout = async () => {
    await logout();
    toast(t("toast.loggedOut"), "info");
    navigate("/welcome", { replace: true });
  };

  return (
    <section className="app-screen space-y-5">
      <PageHeader title={t("profile.title")} />

      <div className="profile-hero card text-center">
        <div className="profile-avatar mx-auto">{initials(user.fullName)}</div>
        <p className="mt-3 text-lg font-semibold">{user.fullName}</p>
        <p className="text-sm text-secondary">{user.email}</p>
      </div>

      <ul className="profile-menu">
        {menuItems.map(({ to, labelKey, icon: Icon, disabled }) => (
          <li key={labelKey}>
            {to && !disabled ? (
              <Link to={to} className="profile-menu-item">
                <Icon className="h-5 w-5 text-accent" strokeWidth={1.75} />
                <span className="flex-1">{t(labelKey)}</span>
                <ChevronRight className="h-4 w-4 text-secondary" />
              </Link>
            ) : (
              <span className="profile-menu-item profile-menu-item--disabled">
                <Icon className="h-5 w-5 text-secondary" strokeWidth={1.75} />
                <span className="flex-1">{t(labelKey)}</span>
                <span className="text-xs text-secondary">{t("common.comingSoon")}</span>
              </span>
            )}
          </li>
        ))}

        <li className="profile-menu-item profile-menu-item--preferences">
          <Globe className="h-5 w-5 shrink-0 text-accent" strokeWidth={1.75} />
          <div className="min-w-0 flex-1">
            <p className="font-medium">{t("profile.language")}</p>
            <p className="text-xs text-secondary">{t("profile.languageHint")}</p>
          </div>
          <LanguageSwitcher compact />
        </li>

        <li className="profile-menu-item profile-menu-item--preferences">
          <div className="min-w-0 flex-1">
            <p className="font-medium">{t("profile.theme")}</p>
            <p className="text-xs text-secondary">{t("profile.themeHint")}</p>
          </div>
          <ThemeToggle variant="switch" />
        </li>
      </ul>

      <button className="btn-secondary w-full flex items-center justify-center gap-2" onClick={() => void handleLogout()} type="button">
        <LogOut className="h-4 w-4" />
        {t("auth.logout")}
      </button>
    </section>
  );
}
