import { z } from "zod";
import { userProfileSchema } from "../auth/schemas";

export const invoiceSummarySchema = z.object({
  id: z.string().uuid(),
  invoiceNumber: z.string(),
  status: z.number().int(),
  clientId: z.string().uuid(),
  clientCompanyName: z.string(),
  invoiceDate: z.string(),
  dueDate: z.string(),
  total: z.number(),
  createdAt: z.string(),
});

export const invoiceLineItemSchema = z.object({
  id: z.string().uuid(),
  itemId: z.string().uuid().nullable().optional(),
  description: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  lineTotal: z.number(),
});

export const invoiceDetailSchema = invoiceSummarySchema.extend({
  clientContactName: z.string(),
  clientEmail: z.string(),
  subtotal: z.number(),
  taxRate: z.number(),
  taxAmount: z.number(),
  notes: z.string().nullable().optional(),
  updatedAt: z.string().nullable().optional(),
  lineItems: z.array(invoiceLineItemSchema),
});

export const paymentRecordSchema = z.object({
  id: z.string().uuid(),
  invoiceId: z.string().uuid(),
  invoiceNumber: z.string(),
  amount: z.number(),
  method: z.number().int(),
  status: z.number().int(),
  paymentDate: z.string(),
  reference: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
});

export const clientResponseSchema = z.object({
  id: z.string().uuid(),
  companyName: z.string(),
  contactName: z.string(),
  email: z.string(),
  phoneNumber: z.string().nullable().optional(),
  address: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  taxNumber: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  isActive: z.boolean(),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
});

export const itemResponseSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  description: z.string().nullable().optional(),
  unitPrice: z.number(),
  currency: z.string(),
  vatRate: z.number(),
  category: z.string().nullable().optional(),
  unit: z.string().nullable().optional(),
  isActive: z.boolean(),
  isArchived: z.boolean(),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
});

export const persistedSessionSchema = z.object({
  accessToken: z.string().min(1),
  refreshToken: z.string().min(1),
  accessTokenExpiresAt: z.string(),
  user: userProfileSchema,
});

export const auditEventSchema = z.object({
  id: z.string().uuid(),
  actorUserId: z.string().uuid(),
  actorDisplayName: z.string(),
  entityType: z.number().int(),
  entityId: z.string().uuid(),
  action: z.number().int(),
  summary: z.string(),
  createdAt: z.string(),
});
