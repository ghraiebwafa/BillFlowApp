import { env } from "../config/env";
import type {
  AuthResponse,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  UserProfile,
} from "../../domain/auth/types";
import { authResponseSchema, messageResponseSchema, userProfileSchema } from "../../domain/auth/schemas";
import { normalizeRole } from "../auth/role-utils";
import { requestJson } from "./http-client";

const base = `${env.authApiUrl}/api/v1.0/auth/account`;

function mapAuthResponse(data: unknown): AuthResponse {
  const parsed = authResponseSchema.parse(data);
  return {
    ...parsed,
    user: { ...parsed.user, role: normalizeRole(parsed.user.role) },
  };
}

function mapProfile(data: unknown): UserProfile {
  const parsed = userProfileSchema.parse(data);
  return { ...parsed, role: normalizeRole(parsed.role) };
}

export const authApi = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    const data = await requestJson<unknown>(`${base}/login`, { method: "POST", body: payload });
    return mapAuthResponse(data);
  },

  async register(payload: RegisterRequest): Promise<MessageResponse> {
    const data = await requestJson<unknown>(`${base}/register`, { method: "POST", body: payload });
    return messageResponseSchema.parse(data);
  },

  async refresh(refreshToken: string): Promise<AuthResponse> {
    const data = await requestJson<unknown>(`${base}/refresh-token`, {
      method: "POST",
      body: { refreshToken },
    });
    return mapAuthResponse(data);
  },

  async profile(accessToken: string): Promise<UserProfile> {
    const data = await requestJson<unknown>(`${base}/profile`, { token: accessToken });
    return mapProfile(data);
  },

  async logout(accessToken: string, refreshToken: string): Promise<MessageResponse> {
    const data = await requestJson<unknown>(`${base}/logout`, {
      method: "POST",
      token: accessToken,
      body: { refreshToken },
    });
    return messageResponseSchema.parse(data);
  },
};
