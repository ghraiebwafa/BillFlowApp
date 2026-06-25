import type { AuthTokens, UserProfile } from "../../domain/auth/types";
import { persistedSessionSchema } from "../../domain/billing/schemas";
import { normalizeRole } from "./role-utils";

const STORAGE_KEY = "billflow.session";

export type PersistedSession = AuthTokens & {
  user: UserProfile;
};

export function loadSession(): PersistedSession | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    const parsed = persistedSessionSchema.safeParse(JSON.parse(raw));
    if (!parsed.success) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }

    return {
      ...parsed.data,
      user: { ...parsed.data.user, role: normalizeRole(parsed.data.user.role) },
    };
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
