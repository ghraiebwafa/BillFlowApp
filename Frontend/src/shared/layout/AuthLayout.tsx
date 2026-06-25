import type { PropsWithChildren } from "react";
import { AuthBrandHeader } from "../ui/AuthBrandHeader";

export function AuthLayout({ children }: PropsWithChildren) {
  return (
    <div className="auth-shell min-h-screen bg-surface">
      <div className="auth-shell-inner w-full md:max-w-md">
        <div className="auth-hero">
          <AuthBrandHeader />
        </div>
        <div className="auth-panel">{children}</div>
      </div>
    </div>
  );
}
