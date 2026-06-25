import { env } from "../config/env";
import type {
  AuthResponse,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  UserProfile,
} from "../../domain/auth/types";
import { requestJson } from "./http-client";

const base = `${env.authApiUrl}/api/v1.0/auth/account`;

export const authApi = {
  login(payload: LoginRequest) {
    return requestJson<AuthResponse>(`${base}/login`, { method: "POST", body: payload });
  },

  register(payload: RegisterRequest) {
    return requestJson<MessageResponse>(`${base}/register`, { method: "POST", body: payload });
  },

  refresh(refreshToken: string) {
    return requestJson<AuthResponse>(`${base}/refresh-token`, {
      method: "POST",
      body: { refreshToken },
    });
  },

  profile(accessToken: string) {
    return requestJson<UserProfile>(`${base}/profile`, { token: accessToken });
  },

  logout(accessToken: string, refreshToken: string) {
    return requestJson<MessageResponse>(`${base}/logout`, {
      method: "POST",
      token: accessToken,
      body: { refreshToken },
    });
  },
};
