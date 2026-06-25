export const PaymentStatus = {
  Completed: 1,
  Refunded: 2,
  Cancelled: 3,
} as const;

export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export const PaymentMethod = {
  Cash: 1,
  BankTransfer: 2,
  CreditCard: 3,
  PayPal: 4,
  Stripe: 5,
} as const;

export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod];

export type PaymentRecord = {
  id: string;
  invoiceId: string;
  invoiceNumber: string;
  amount: number;
  method: PaymentMethod;
  status: PaymentStatus;
  paymentDate: string;
  reference?: string | null;
  notes?: string | null;
  createdAt: string;
  updatedAt?: string | null;
};

export function paymentMethodLabel(method: PaymentMethod): string {
  switch (method) {
    case PaymentMethod.BankTransfer:
      return "Bank Transfer";
    case PaymentMethod.CreditCard:
      return "Credit Card";
    case PaymentMethod.PayPal:
      return "PayPal";
    case PaymentMethod.Stripe:
      return "Stripe";
    default:
      return "Cash";
  }
}
