import type { TFunction } from "i18next";
import { InvoiceStatus } from "./invoice";

export function invoiceStatusLabel(status: InvoiceStatus, t: TFunction): string {
  switch (status) {
    case InvoiceStatus.Paid:
      return t("invoiceStatus.paid");
    case InvoiceStatus.PartiallyPaid:
      return t("invoiceStatus.partial");
    case InvoiceStatus.Overdue:
      return t("invoiceStatus.overdue");
    case InvoiceStatus.Sent:
      return t("invoiceStatus.sent");
    case InvoiceStatus.Draft:
      return t("invoiceStatus.draft");
    case InvoiceStatus.Cancelled:
      return t("invoiceStatus.cancelled");
    default:
      return t("invoiceStatus.unknown");
  }
}

export function invoiceStatusClass(status: InvoiceStatus): string {
  switch (status) {
    case InvoiceStatus.Paid:
      return "status-badge--paid";
    case InvoiceStatus.PartiallyPaid:
      return "status-badge--partial";
    case InvoiceStatus.Overdue:
    case InvoiceStatus.Sent:
      return "status-badge--unpaid";
    case InvoiceStatus.Draft:
      return "status-badge--draft";
    default:
      return "status-badge--draft";
  }
}
