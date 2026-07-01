namespace BillFlow.Shared.Email;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
