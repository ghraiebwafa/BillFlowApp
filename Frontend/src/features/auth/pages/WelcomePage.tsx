import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AuthBrandHeader } from "../../../shared/ui/AuthBrandHeader";
import { LanguageSwitcher } from "../../../shared/ui/LanguageSwitcher";
import { ThemeToggle } from "../../../shared/ui/ThemeToggle";

export function WelcomePage() {
  const { t } = useTranslation();

  return (
    <div className="auth-page">
      <div className="auth-page-inner">
        <div className="auth-hero-section">
          <header className="auth-toolbar">
            <LanguageSwitcher />
            <ThemeToggle compact />
          </header>
          <AuthBrandHeader />
        </div>

        <main className="auth-form-panel space-y-5">
          <div>
            <h2 className="text-xl font-semibold">{t("welcome.title")}</h2>
            <p className="mt-1 text-sm text-secondary">{t("welcome.subtitle")}</p>
          </div>

          <Link className="btn-primary block text-center no-underline" to="/login">
            {t("auth.login")}
          </Link>

          <div className="flex items-center gap-3 text-xs text-secondary">
            <span className="h-px flex-1 bg-[var(--border-muted)]" />
            {t("welcome.or")}
            <span className="h-px flex-1 bg-[var(--border-muted)]" />
          </div>

          <Link className="btn-secondary block text-center no-underline" to="/register">
            {t("auth.register")}
          </Link>
        </main>
      </div>
    </div>
  );
}
