import { useTranslation } from "react-i18next";

export function LanguageSwitcher() {
  const { i18n } = useTranslation();

  const change = (lang: "en" | "fr") => {
    void i18n.changeLanguage(lang);
  };

  return (
    <div className="inline-flex gap-1 rounded-md border border-muted p-1">
      <button
        type="button"
        className={`px-2 py-1 text-xs ${i18n.language === "en" ? "bg-accent text-white rounded" : ""}`}
        onClick={() => change("en")}
      >
        EN
      </button>
      <button
        type="button"
        className={`px-2 py-1 text-xs ${i18n.language === "fr" ? "bg-accent text-white rounded" : ""}`}
        onClick={() => change("fr")}
      >
        FR
      </button>
    </div>
  );
}
