import type { TFunction } from "i18next";
import { PaymentMethod, PaymentStatus } from "./payment";

export function paymentMethodLabel(method: PaymentMethod, t: TFunction): string {
  switch (method) {
    case PaymentMethod.BankTransfer:
      return t("paymentMethod.bankTransfer");
    case PaymentMethod.CreditCard:
      return t("paymentMethod.creditCard");
    case PaymentMethod.PayPal:
      return t("paymentMethod.paypal");
    case PaymentMethod.Stripe:
      return t("paymentMethod.stripe");
    default:
      return t("paymentMethod.cash");
  }
}

export function paymentStatusLabel(status: PaymentStatus, t: TFunction): string {
  switch (status) {
    case PaymentStatus.Refunded:
      return t("paymentStatus.refunded");
    case PaymentStatus.Cancelled:
      return t("paymentStatus.cancelled");
    default:
      return t("paymentStatus.completed");
  }
}
