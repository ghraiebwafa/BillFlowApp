import { create } from "zustand";
import { authApi } from "../api/auth-api";
import type { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from "../../domain/auth/types";
import { normalizeRole } from "./role-utils";
import {
  clearSessionStorage,
  isAccessTokenExpired,
  loadSession,
  saveSession,
  type PersistedSession,
} from "./token-storage";

type SessionState = {
  isHydrated: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  accessTokenExpiresAt: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  hydrate: () => Promise<void>;
  setAuth: (response: AuthResponse) => void;
  refreshSession: () => Promise<boolean>;
  login: (payload: LoginRequest) => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  clearSession: () => void;
};

let refreshInFlight: Promise<boolean> | null = null;

function toPersisted(response: AuthResponse): PersistedSession {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    user: {
      ...response.user,
      role: normalizeRole(response.user.role),
    },
  };
}

function applyPersisted(
  set: (state: Partial<SessionState>) => void,
  session: PersistedSession,
) {
  set({
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
    accessTokenExpiresAt: session.accessTokenExpiresAt,
    user: session.user,
    isAuthenticated: true,
  });
}

async function syncProfile(accessToken: string): Promise<UserProfile | null> {
  try {
    const profile = await authApi.profile(accessToken);
    return { ...profile, role: normalizeRole(profile.role) };
  } catch {
    return null;
  }
}

export const useSessionStore = create<SessionState>((set, get) => ({
  isHydrated: false,
  accessToken: null,
  refreshToken: null,
  accessTokenExpiresAt: null,
  user: null,
  isAuthenticated: false,

  setAuth: (response) => {
    const session = toPersisted(response);
    saveSession(session);
    applyPersisted(set, session);
  },

  clearSession: () => {
    clearSessionStorage();
    set({
      accessToken: null,
      refreshToken: null,
      accessTokenExpiresAt: null,
      user: null,
      isAuthenticated: false,
    });
  },

  refreshSession: async () => {
    if (refreshInFlight) {
      return refreshInFlight;
    }

    refreshInFlight = (async () => {
      const { refreshToken } = get();
      if (!refreshToken) return false;

      try {
        const response = await authApi.refresh(refreshToken);
        get().setAuth(response);
        return true;
      } catch {
        return false;
      } finally {
        refreshInFlight = null;
      }
    })();

    return refreshInFlight;
  },

  hydrate: async () => {
    const stored = loadSession();
    if (!stored) {
      set({ isHydrated: true });
      return;
    }

    applyPersisted(set, stored);

    const tokenValid = isAccessTokenExpired(stored.accessTokenExpiresAt)
      ? await get().refreshSession()
      : true;

    if (!tokenValid) {
      get().clearSession();
      set({ isHydrated: true });
      return;
    }

    const { accessToken } = get();
    if (accessToken) {
      const profile = await syncProfile(accessToken);
      if (profile) {
        const session = loadSession();
        if (session) {
          const updated = { ...session, user: profile };
          saveSession(updated);
          set({ user: profile });
        }
      }
    }

    set({ isHydrated: true });
  },

  login: async (payload) => {
    const response = await authApi.login(payload);
    get().setAuth(response);
  },

  register: async (payload) => {
    await authApi.register(payload);
    await get().login({ email: payload.email, password: payload.password });
  },

  logout: async () => {
    const { accessToken, refreshToken } = get();
    if (accessToken && refreshToken) {
      try {
        await authApi.logout(accessToken, refreshToken);
      } catch {
        // clear local session even if remote logout fails
      }
    }

    get().clearSession();
  },
}));
