import { env } from "../config/env";
import { ApiError, parseApiError } from "./api-error";
import { isUnauthorized } from "./http-client";
import { useSessionStore } from "../auth/session-store";

type ManagementRequestOptions = {
  method?: string;
  body?: unknown;
  retryOnUnauthorized?: boolean;
};

export async function managementRequest<T>(
  path: string,
  options: ManagementRequestOptions = {},
): Promise<T> {
  const url = `${env.managementApiUrl}${path}`;
  const retryOnUnauthorized = options.retryOnUnauthorized ?? true;

  const execute = async (): Promise<T> => {
    const { accessToken } = useSessionStore.getState();
    if (!accessToken) {
      throw new ApiError("Authentication required.", 401);
    }

    const headers: Record<string, string> = {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    };

    if (options.body !== undefined) {
      headers["Content-Type"] = "application/json";
    }

    const response = await fetch(url, {
      method: options.method ?? (options.body !== undefined ? "POST" : "GET"),
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });

    if (!response.ok) {
      throw await parseApiError(response);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      return (await response.json()) as T;
    }

    return (await response.blob()) as T;
  };

  try {
    return await execute();
  } catch (error) {
    if (!retryOnUnauthorized || !isUnauthorized(error)) {
      throw error;
    }

    const refreshed = await useSessionStore.getState().refreshSession();
    if (!refreshed) {
      useSessionStore.getState().clearSession();
      throw error;
    }

    return execute();
  }
}
