import type { PropsWithChildren } from "react";
import { AuthBrandHeader } from "../ui/AuthBrandHeader";
import { LanguageSwitcher } from "../ui/LanguageSwitcher";
import { ThemeToggle } from "../ui/ThemeToggle";

export function AuthLayout({ children }: PropsWithChildren) {
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
        <main className="auth-form-panel">{children}</main>
      </div>
    </div>
  );
}
