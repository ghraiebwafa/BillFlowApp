import { managementRequest } from "../../shared/api/management-client";
import { ApiError } from "../../shared/api/api-error";
import { billingApi } from "./api-paths";
import type { CompanySettingsResponse } from "./company-settings";

export const companySettingsQueryKey = ["company-settings"] as const;

/** Shared fetcher: missing settings (404) resolves to null so React Query cache stays consistent. */
export async function fetchCompanySettings(): Promise<CompanySettingsResponse | null> {
  try {
    return await managementRequest<CompanySettingsResponse>(billingApi.companySettings);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}
