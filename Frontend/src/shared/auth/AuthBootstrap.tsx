import type { PropsWithChildren } from "react";
import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { useSessionStore } from "./session-store";

export function AuthBootstrap({ children }: PropsWithChildren) {
  const { t } = useTranslation();
  const { isHydrated, hydrate } = useSessionStore();

  useEffect(() => {
    void hydrate();
  }, [hydrate]);

  if (!isHydrated) {
    return (
      <div className="grid min-h-screen place-items-center bg-surface text-primary">
        <p className="text-secondary">{t("app.loading")}</p>
      </div>
    );
  }

  return children;
}
