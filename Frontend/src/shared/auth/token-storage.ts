import type { AuthTokens, UserProfile } from "../../domain/auth/types";

const STORAGE_KEY = "billflow.session";

export type PersistedSession = AuthTokens & {
  user: UserProfile;
};

export function loadSession(): PersistedSession | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as PersistedSession;
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function saveSession(session: PersistedSession): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearSessionStorage(): void {
  sessionStorage.removeItem(STORAGE_KEY);
}

export function isAccessTokenExpired(expiresAt: string): boolean {
  return new Date(expiresAt).getTime() <= Date.now() + 30_000;
}
