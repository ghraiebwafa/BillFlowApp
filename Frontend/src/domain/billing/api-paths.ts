const billing = "/api/v1.0/billing";

export const billingApi = {
  clients: `${billing}/clients`,
  client: (id: string) => `${billing}/clients/${id}`,

  items: `${billing}/items`,
  item: (id: string) => `${billing}/items/${id}`,
  itemArchive: (id: string) => `${billing}/items/${id}/archive`,

  invoices: `${billing}/invoices`,
  invoice: (id: string) => `${billing}/invoices/${id}`,
  invoicePayments: (id: string) => `${billing}/invoices/${id}/payments`,
  invoiceSend: (id: string) => `${billing}/invoices/${id}/send`,
  invoiceEmail: (id: string) => `${billing}/invoices/${id}/email`,
  invoiceMarkPaid: (id: string) => `${billing}/invoices/${id}/mark-paid`,
  invoiceCancel: (id: string) => `${billing}/invoices/${id}/cancel`,
  invoicePdf: (id: string) => `${billing}/invoices/${id}/pdf`,
  invoiceShareLink: (id: string) => `${billing}/invoices/${id}/share-link`,
  invoiceDuplicate: (id: string) => `${billing}/invoices/${id}/duplicate`,

  payments: `${billing}/payments`,
  paymentRefund: (id: string) => `${billing}/payments/${id}/refund`,
  paymentCancel: (id: string) => `${billing}/payments/${id}/cancel`,

  dashboard: `${billing}/dashboard`,
  companySettings: `${billing}/company-settings`,
  activity: (limit = 50) => `${billing}/activity?limit=${limit}`,

  reports: {
    sales: `${billing}/reports/sales`,
    payments: `${billing}/reports/payments`,
    outstanding: `${billing}/reports/outstanding`,
    taxes: `${billing}/reports/taxes`,
  },
} as const;

export const portalApi = {
  invoice: (token: string) => `/api/v1.0/portal/${encodeURIComponent(token)}`,
  invoicePdf: (token: string) => `/api/v1.0/portal/${encodeURIComponent(token)}/pdf`,
} as const;
