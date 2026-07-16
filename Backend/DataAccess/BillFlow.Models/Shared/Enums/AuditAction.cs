namespace BillFlow.Models.Shared.Enums;

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Sent = 4,
    Cancelled = 5,
    Paid = 6,
    PaymentRecorded = 7,
    Refunded = 8,
    Archived = 9,
    SettingsUpdated = 10,
    EmailSent = 11,
    ShareLinkCreated = 12,
    ShareLinkRevoked = 13,
    PortalViewed = 14,
    PortalPdfDownloaded = 15,
    PortalCheckoutStarted = 16,
}
