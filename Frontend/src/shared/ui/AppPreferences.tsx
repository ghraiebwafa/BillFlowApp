import { LanguageSwitcher } from "../ui/LanguageSwitcher";
import { ThemeToggle } from "../ui/ThemeToggle";

type AppPreferencesProps = {
  layout?: "stack" | "row";
};

/** Language + theme controls for authenticated app surfaces (sidebar, profile). */
export function AppPreferences({ layout = "stack" }: AppPreferencesProps) {
  return (
    <div className={layout === "row" ? "app-preferences app-preferences--row" : "app-preferences"}>
      <LanguageSwitcher />
      <ThemeToggle variant="switch" />
    </div>
  );
}
