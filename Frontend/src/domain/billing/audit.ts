export const AuditAction = {
  Created: 1,
  Updated: 2,
  Deleted: 3,
  Sent: 4,
  Cancelled: 5,
  Paid: 6,
  PaymentRecorded: 7,
  Refunded: 8,
  Archived: 9,
  SettingsUpdated: 10,
  EmailSent: 11,
} as const;

export type AuditAction = (typeof AuditAction)[keyof typeof AuditAction];

export const AuditEntityType = {
  Client: 1,
  Item: 2,
  Invoice: 3,
  Payment: 4,
  CompanySettings: 5,
} as const;

export type AuditEntityType = (typeof AuditEntityType)[keyof typeof AuditEntityType];

export type AuditEvent = {
  id: string;
  actorUserId: string;
  actorDisplayName: string;
  entityType: AuditEntityType;
  entityId: string;
  action: AuditAction;
  summary: string;
  createdAt: string;
};

export function auditActionLabel(action: AuditAction): string {
  switch (action) {
    case AuditAction.Created:
      return "Created";
    case AuditAction.Updated:
      return "Updated";
    case AuditAction.Deleted:
      return "Deleted";
    case AuditAction.Sent:
      return "Sent";
    case AuditAction.Cancelled:
      return "Cancelled";
    case AuditAction.Paid:
      return "Paid";
    case AuditAction.PaymentRecorded:
      return "Payment";
    case AuditAction.Refunded:
      return "Refunded";
    case AuditAction.Archived:
      return "Archived";
    case AuditAction.SettingsUpdated:
      return "Settings";
    case AuditAction.EmailSent:
      return "Email";
    default:
      return "Activity";
  }
}

export function auditEntityLabel(entityType: AuditEntityType): string {
  switch (entityType) {
    case AuditEntityType.Client:
      return "Client";
    case AuditEntityType.Item:
      return "Item";
    case AuditEntityType.Invoice:
      return "Invoice";
    case AuditEntityType.Payment:
      return "Payment";
    case AuditEntityType.CompanySettings:
      return "Settings";
    default:
      return "Record";
  }
}
