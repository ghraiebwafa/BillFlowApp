import { env } from "../config/env";
import { useSessionStore } from "../auth/session-store";
import { ApiError } from "./api-error";

async function fetchWithAuth(path: string): Promise<Response> {
  const url = `${env.managementApiUrl}${path}`;

  const execute = async (): Promise<Response> => {
    const { accessToken } = useSessionStore.getState();
    if (!accessToken) {
      throw new ApiError("Authentication required.", 401);
    }

    return fetch(url, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
  };

  let response = await execute();
  if (response.status === 401) {
    const refreshed = await useSessionStore.getState().refreshSession();
    if (!refreshed) {
      useSessionStore.getState().clearSession();
      throw new ApiError("Session expired.", 401);
    }

    response = await execute();
  }

  if (!response.ok) {
    throw new ApiError("Download failed.", response.status);
  }

  return response;
}

export async function downloadWithAuth(path: string, filename: string): Promise<void> {
  const response = await fetchWithAuth(path);
  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(objectUrl);
}
