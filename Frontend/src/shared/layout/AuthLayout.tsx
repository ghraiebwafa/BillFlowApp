import type { PropsWithChildren } from "react";
import { BillFlowLogo } from "../ui/BillFlowLogo";

export function AuthLayout({ children }: PropsWithChildren) {
  return (
    <div className="auth-page auth-page--form">
      <header className="auth-form-header">
        <BillFlowLogo size="auth" />
      </header>
      <main className="auth-form-main">{children}</main>
    </div>
  );
}
