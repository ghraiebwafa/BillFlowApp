const authApiUrl = import.meta.env.VITE_AUTH_API_URL ?? "http://localhost:5237";
const managementApiUrl = import.meta.env.VITE_MANAGEMENT_API_URL ?? "http://localhost:5177";

export const env = {
  authApiUrl: authApiUrl.replace(/\/$/, ""),
  managementApiUrl: managementApiUrl.replace(/\/$/, ""),
};
