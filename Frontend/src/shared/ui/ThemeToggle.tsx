import { useEffect, useState } from "react";
import { Moon, Sun } from "lucide-react";
import { useTranslation } from "react-i18next";

type Theme = "light" | "dark";

type ThemeToggleProps = {
  /** switch = iOS-style track; inline = compact row for menus */
  variant?: "switch" | "inline";
};

export function ThemeToggle({ variant = "switch" }: ThemeToggleProps) {
  const { t } = useTranslation();
  const [theme, setTheme] = useState<Theme>("light");

  useEffect(() => {
    const saved = localStorage.getItem("theme") as Theme | null;
    const next = saved ?? "light";
    setTheme(next);
    document.documentElement.dataset.theme = next;
  }, []);

  const toggle = () => {
    const next: Theme = theme === "light" ? "dark" : "light";
    setTheme(next);
    localStorage.setItem("theme", next);
    document.documentElement.dataset.theme = next;
  };

  const isDark = theme === "dark";
  const label = isDark ? t("profile.themeDark") : t("profile.themeLight");

  if (variant === "inline") {
    return (
      <button
        className="theme-inline-btn"
        onClick={toggle}
        type="button"
        aria-label={label}
        title={label}
      >
        {isDark ? <Moon className="h-4 w-4" /> : <Sun className="h-4 w-4" />}
        <span>{label}</span>
      </button>
    );
  }

  return (
    <button
      className={`theme-switch ${isDark ? "theme-switch--dark" : ""}`}
      onClick={toggle}
      type="button"
      role="switch"
      aria-checked={isDark}
      aria-label={label}
    >
      <span className="theme-switch-track">
        <span className="theme-switch-thumb">
          {isDark ? <Moon className="h-3.5 w-3.5" /> : <Sun className="h-3.5 w-3.5" />}
        </span>
      </span>
      <span className="theme-switch-label">{label}</span>
    </button>
  );
}
