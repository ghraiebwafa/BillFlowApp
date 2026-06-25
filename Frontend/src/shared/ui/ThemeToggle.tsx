import { useEffect, useState } from "react";

type Theme = "light" | "dark";

type ThemeToggleProps = {
  compact?: boolean;
};

export function ThemeToggle({ compact = false }: ThemeToggleProps) {
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

  if (compact) {
    return (
      <button className="auth-chip" onClick={toggle} type="button">
        {theme === "light" ? "Dark" : "Light"}
      </button>
    );
  }

  return (
    <button className="btn-secondary" onClick={toggle} type="button">
      {theme === "light" ? "Dark" : "Light"}
    </button>
  );
}
