export type CompanySettingsResponse = {
  companyName: string;
  address?: string | null;
  country?: string | null;
  taxNumber?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  currency: string;
  invoiceNumberPrefix: string;
  defaultTaxRate: number;
  paymentTermsDays: number;
  timeZone?: string | null;
  brandColor?: string | null;
  invoiceFooterNote?: string | null;
  hasLogo?: boolean;
  enablePaymentReminders?: boolean;
  reminderDaysBeforeDue?: number;
  createdAt: string;
  updatedAt?: string | null;
};

export type UpsertCompanySettingsRequest = {
  companyName: string;
  address?: string;
  country?: string;
  taxNumber?: string;
  phoneNumber?: string;
  email?: string;
  currency: string;
  invoiceNumberPrefix: string;
  defaultTaxRate: number;
  paymentTermsDays: number;
  timeZone?: string;
  brandColor?: string;
  invoiceFooterNote?: string;
  enablePaymentReminders?: boolean;
  reminderDaysBeforeDue?: number;
};

export const defaultCompanySettingsForm = {
  companyName: "",
  address: "",
  country: "",
  taxNumber: "",
  phoneNumber: "",
  email: "",
  currency: "USD",
  invoiceNumberPrefix: "INV",
  defaultTaxRate: 0,
  paymentTermsDays: 30,
  timeZone: "",
  brandColor: "#FF6B00",
  invoiceFooterNote: "",
  enablePaymentReminders: false,
  reminderDaysBeforeDue: 3,
};

export function mapSettingsToForm(
  settings: CompanySettingsResponse,
): typeof defaultCompanySettingsForm {
  return {
    companyName: settings.companyName,
    address: settings.address ?? "",
    country: settings.country ?? "",
    taxNumber: settings.taxNumber ?? "",
    phoneNumber: settings.phoneNumber ?? "",
    email: settings.email ?? "",
    currency: settings.currency,
    invoiceNumberPrefix: settings.invoiceNumberPrefix,
    defaultTaxRate: settings.defaultTaxRate,
    paymentTermsDays: settings.paymentTermsDays,
    timeZone: settings.timeZone ?? "",
    brandColor: settings.brandColor ?? "#FF6B00",
    invoiceFooterNote: settings.invoiceFooterNote ?? "",
    enablePaymentReminders: settings.enablePaymentReminders ?? false,
    reminderDaysBeforeDue: settings.reminderDaysBeforeDue ?? 3,
  };
}

export function mapFormToRequest(
  values: typeof defaultCompanySettingsForm,
): UpsertCompanySettingsRequest {
  const optional = (value: string) => {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  };

  return {
    companyName: values.companyName.trim(),
    address: optional(values.address),
    country: optional(values.country),
    taxNumber: optional(values.taxNumber),
    phoneNumber: optional(values.phoneNumber),
    email: optional(values.email),
    currency: values.currency.trim().toUpperCase(),
    invoiceNumberPrefix: values.invoiceNumberPrefix.trim(),
    defaultTaxRate: values.defaultTaxRate,
    paymentTermsDays: values.paymentTermsDays,
    timeZone: optional(values.timeZone),
    brandColor: optional(values.brandColor),
    invoiceFooterNote: optional(values.invoiceFooterNote),
    enablePaymentReminders: values.enablePaymentReminders,
    reminderDaysBeforeDue: values.reminderDaysBeforeDue,
  };
}
