import { useQuery } from "@tanstack/react-query";
import { managementRequest } from "../api/management-client";
import { billingApi } from "../../domain/billing/api-paths";
import type { CompanySettingsResponse } from "../../domain/billing/company-settings";

export function useCompanyCurrency(): string {
  const { data } = useQuery({
    queryKey: ["company-settings"],
    queryFn: () => managementRequest<CompanySettingsResponse>(billingApi.companySettings),
    staleTime: 60_000,
  });

  return data?.currency?.trim() || "USD";
}

export function formatMoney(
  amount: number,
  currency = "USD",
  options?: { maximumFractionDigits?: number },
): string {
  const currencyCode = currency.trim() || "USD";
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: currencyCode,
      maximumFractionDigits: options?.maximumFractionDigits,
    }).format(amount);
  } catch {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: "USD",
      maximumFractionDigits: options?.maximumFractionDigits,
    }).format(amount);
  }
}
