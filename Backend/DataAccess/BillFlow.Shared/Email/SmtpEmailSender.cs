using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BillFlow.Shared.Email;

public sealed class SmtpEmailSender(SmtpOptions options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public bool IsConfigured => options.IsConfigured;

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
        {
            return new EmailSendResult(
                false,
                Skipped: true,
                Detail: "SMTP is not configured.");
        }

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
            mime.To.Add(new MailboxAddress(message.ToName ?? message.ToEmail, message.ToEmail));
            mime.Subject = message.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.PlainTextBody ?? StripHtml(message.HtmlBody),
            };

            foreach (var attachment in message.Attachments)
            {
                builder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }

            mime.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                options.Host,
                options.Port,
                options.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                if (string.IsNullOrWhiteSpace(options.Password))
                {
                    return new EmailSendResult(
                        false,
                        Skipped: false,
                        Detail: "SMTP password is required when username is configured.");
                }

                await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult(true, Skipped: false, Detail: "Email sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {ToEmail}", message.ToEmail);
            return new EmailSendResult(false, Skipped: false, Detail: "Unable to send invoice email.");
        }
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
}
