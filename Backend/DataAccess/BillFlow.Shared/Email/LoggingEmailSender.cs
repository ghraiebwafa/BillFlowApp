using Microsoft.Extensions.Logging;

namespace BillFlow.Shared.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public bool IsConfigured => false;

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email delivery skipped (SMTP not configured). To={ToEmail} Subject={Subject} Attachments={AttachmentCount}",
            message.ToEmail,
            message.Subject,
            message.Attachments.Count);

        return Task.FromResult(new EmailSendResult(
            Success: true,
            Skipped: true,
            Detail: "SMTP is not configured. Invoice was processed without email delivery."));
    }
}
