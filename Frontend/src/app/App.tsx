import { AppProviders } from "./providers";
import { AppRouter } from "./router";
import { AuthBootstrap } from "../shared/auth/AuthBootstrap";

export function App() {
  return (
    <AppProviders>
      <AuthBootstrap>
        <AppRouter />
      </AuthBootstrap>
    </AppProviders>
  );
}
