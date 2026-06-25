function trimTrailingSlash(value: string): string {
  return value.replace(/\/$/, "");
}

function resolveUrl(name: string, value: string | undefined, devFallback: string): string {
  if (import.meta.env.PROD && !value) {
    throw new Error(`${name} must be set in production builds.`);
  }

  return trimTrailingSlash(value ?? devFallback);
}

export const env = {
  authApiUrl: resolveUrl("VITE_AUTH_API_URL", import.meta.env.VITE_AUTH_API_URL, "http://localhost:5237"),
  managementApiUrl: resolveUrl(
    "VITE_MANAGEMENT_API_URL",
    import.meta.env.VITE_MANAGEMENT_API_URL,
    "http://localhost:5177",
  ),
};
