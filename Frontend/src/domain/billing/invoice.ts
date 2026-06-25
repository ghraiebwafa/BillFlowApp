export const InvoiceStatus = {
  Draft: 1,
  Sent: 2,
  Paid: 3,
  Overdue: 4,
  Cancelled: 5,
  PartiallyPaid: 6,
} as const;

export type InvoiceStatus = (typeof InvoiceStatus)[keyof typeof InvoiceStatus];

export type InvoiceSummary = {
  id: string;
  invoiceNumber: string;
  status: InvoiceStatus;
  clientId: string;
  clientCompanyName: string;
  invoiceDate: string;
  dueDate: string;
  total: number;
  createdAt: string;
};

export type InvoiceLineItem = {
  id: string;
  itemId?: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type InvoiceDetail = {
  id: string;
  invoiceNumber: string;
  status: InvoiceStatus;
  clientId: string;
  clientCompanyName: string;
  clientContactName: string;
  clientEmail: string;
  invoiceDate: string;
  dueDate: string;
  subtotal: number;
  taxRate: number;
  taxAmount: number;
  total: number;
  notes?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  lineItems: InvoiceLineItem[];
};

export function invoiceStatusLabel(status: InvoiceStatus): string {
  switch (status) {
    case InvoiceStatus.Paid:
      return "Paid";
    case InvoiceStatus.PartiallyPaid:
      return "Partial";
    case InvoiceStatus.Overdue:
      return "Unpaid";
    case InvoiceStatus.Sent:
      return "Sent";
    case InvoiceStatus.Draft:
      return "Draft";
    case InvoiceStatus.Cancelled:
      return "Cancelled";
    default:
      return "Unknown";
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
