import { useState } from "react";
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
import { FormField } from "../../../shared/ui/FormField";
import { useSessionStore } from "../../../shared/auth/session-store";
import { authApi } from "../../../shared/api/auth-api";
import { ApiError } from "../../../shared/api/api-error";
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
  const { user, accessToken, logout } = useSessionStore();
  const [passwordOpen, setPasswordOpen] = useState(false);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [savingPassword, setSavingPassword] = useState(false);

  if (!user) return null;

  const menuItems: Array<{
    to: string | null;
    labelKey: string;
    icon: typeof Building2;
    disabled?: boolean;
    onClick?: () => void;
  }> = [
    { to: "/settings/company", labelKey: "profile.companySettings", icon: Building2 },
    { to: "/activity", labelKey: "profile.activity", icon: History },
    { to: null, labelKey: "profile.myProfile", icon: User, disabled: true },
    {
      to: null,
      labelKey: "profile.changePassword",
      icon: KeyRound,
      onClick: () => {
        setPasswordOpen((open) => !open);
        setPasswordError(null);
      },
    },
    { to: null, labelKey: "profile.preferences", icon: SlidersHorizontal, disabled: true },
    { to: null, labelKey: "profile.notifications", icon: Bell, disabled: true },
  ];

  const handleLogout = async () => {
    await logout();
    toast(t("toast.loggedOut"), "info");
    navigate("/welcome", { replace: true });
  };

  const handleChangePassword = async () => {
    if (!accessToken) return;
    setPasswordError(null);

    if (newPassword.length < 8) {
      setPasswordError(t("profile.passwordTooShort"));
      return;
    }
    if (newPassword !== confirmNewPassword) {
      setPasswordError(t("profile.passwordMismatch"));
      return;
    }

    setSavingPassword(true);
    try {
      await authApi.changePassword(accessToken, {
        currentPassword,
        newPassword,
        confirmNewPassword,
      });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmNewPassword("");
      setPasswordOpen(false);
      toast(t("toast.passwordChanged"), "success");
    } catch (err) {
      setPasswordError(err instanceof ApiError ? err.message : t("common.actionFailed"));
    } finally {
      setSavingPassword(false);
    }
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
        {menuItems.map(({ to, labelKey, icon: Icon, disabled, onClick }) => (
          <li key={labelKey}>
            {to && !disabled ? (
              <Link to={to} className="profile-menu-item">
                <Icon className="h-5 w-5 text-accent" strokeWidth={1.75} />
                <span className="flex-1">{t(labelKey)}</span>
                <ChevronRight className="h-4 w-4 text-secondary" />
              </Link>
            ) : onClick ? (
              <button className="profile-menu-item w-full text-left" type="button" onClick={onClick}>
                <Icon className="h-5 w-5 text-accent" strokeWidth={1.75} />
                <span className="flex-1">{t(labelKey)}</span>
                <ChevronRight className="h-4 w-4 text-secondary" />
              </button>
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

      {passwordOpen ? (
        <div className="card space-y-3">
          <h3 className="font-semibold">{t("profile.changePassword")}</h3>
          <FormField
            label={t("profile.currentPassword")}
            type="password"
            showPasswordToggle
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            autoComplete="current-password"
          />
          <FormField
            label={t("profile.newPassword")}
            type="password"
            showPasswordToggle
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password"
          />
          <FormField
            label={t("profile.confirmNewPassword")}
            type="password"
            showPasswordToggle
            value={confirmNewPassword}
            onChange={(e) => setConfirmNewPassword(e.target.value)}
            autoComplete="new-password"
          />
          {passwordError ? (
            <p className="text-sm text-red-500" role="alert">
              {passwordError}
            </p>
          ) : null}
          <div className="flex gap-2">
            <button
              className="btn-secondary flex-1"
              type="button"
              onClick={() => {
                setPasswordOpen(false);
                setPasswordError(null);
              }}
            >
              {t("clients.cancel")}
            </button>
            <button
              className="btn-primary flex-1"
              type="button"
              disabled={savingPassword}
              onClick={() => void handleChangePassword()}
            >
              {savingPassword ? t("app.loading") : t("profile.savePassword")}
            </button>
          </div>
        </div>
      ) : null}

      <button className="btn-secondary w-full flex items-center justify-center gap-2" onClick={() => void handleLogout()} type="button">
        <LogOut className="h-4 w-4" />
        {t("auth.logout")}
      </button>
    </section>
  );
}
