import { useTranslation } from "react-i18next";

type LanguageSwitcherProps = {
  /** compact labels for profile menu row; default shows full language names */
  compact?: boolean;
};

export function LanguageSwitcher({ compact = false }: LanguageSwitcherProps) {
  const { i18n, t } = useTranslation();
  const current = i18n.language.startsWith("fr") ? "fr" : "en";

  const change = (lang: "en" | "fr") => {
    void i18n.changeLanguage(lang);
  };

  return (
    <div className="lang-switch" role="group" aria-label={t("profile.language")}>
      <button
        type="button"
        className={current === "en" ? "lang-switch-btn active" : "lang-switch-btn"}
        onClick={() => change("en")}
        aria-pressed={current === "en"}
      >
        {compact ? "EN" : t("profile.langEn")}
      </button>
      <button
        type="button"
        className={current === "fr" ? "lang-switch-btn active" : "lang-switch-btn"}
        onClick={() => change("fr")}
        aria-pressed={current === "fr"}
      >
        {compact ? "FR" : t("profile.langFr")}
      </button>
    </div>
  );
}
