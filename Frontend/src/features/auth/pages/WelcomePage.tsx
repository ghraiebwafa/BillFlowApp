import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { BillFlowLogo } from "../../../shared/ui/BillFlowLogo";

export function WelcomePage() {
  const { t } = useTranslation();

  return (
    <div className="welcome-splash">
      <div className="welcome-splash-body">
        <BillFlowLogo size="splash" />
        <p className="welcome-splash-tagline">{t("app.kicker")}</p>
      </div>

      <footer className="welcome-splash-footer">
        <Link className="btn-primary btn-primary--lg block text-center no-underline" to="/login">
          {t("welcome.getStarted")}
        </Link>
        <p className="mt-4 text-center text-sm text-secondary">
          {t("auth.noAccount")}{" "}
          <Link to="/register" className="font-semibold text-accent no-underline">
            {t("auth.register")}
          </Link>
        </p>
      </footer>
    </div>
  );
}
