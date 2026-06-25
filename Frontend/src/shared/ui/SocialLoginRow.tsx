import { useTranslation } from "react-i18next";

export function SocialLoginRow() {
  const { t } = useTranslation();

  return (
    <div className="social-login">
      <div className="social-login-divider">
        <span>{t("auth.orContinueWith")}</span>
      </div>
      <div className="social-login-buttons">
        <button className="social-btn" type="button" disabled title={t("common.comingSoon")}>
          <span className="social-btn-icon social-btn-icon--google" aria-hidden>G</span>
          Google
        </button>
        <button className="social-btn" type="button" disabled title={t("common.comingSoon")}>
          <span className="social-btn-icon social-btn-icon--apple" aria-hidden>
            &#63743;
          </span>
          Apple
        </button>
      </div>
    </div>
  );
}
