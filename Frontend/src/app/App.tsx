import { AppProviders } from "./providers";
import { AppRouter } from "./router";
import { AuthBootstrap } from "../shared/auth/AuthBootstrap";
import { ErrorBoundary } from "../shared/ui/ErrorBoundary";

export function App() {
  return (
    <ErrorBoundary>
      <AppProviders>
        <AuthBootstrap>
          <AppRouter />
        </AuthBootstrap>
      </AppProviders>
    </ErrorBoundary>
  );
}
