namespace BillFlow.Shared.Email;

public static class AuthEmailComposer
{
    public static EmailMessage PasswordReset(string toEmail, string toName, string resetUrl) =>
        new()
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = "Reset your BillFlow password",
            PlainTextBody =
                $"Hi {toName},\n\nReset your password using this link (valid for 1 hour):\n{resetUrl}\n\nIf you did not request this, ignore this email.",
            HtmlBody =
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(toName)},</p>"
                + "<p>Reset your BillFlow password using the link below (valid for 1 hour):</p>"
                + $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(resetUrl)}\">Reset password</a></p>"
                + "<p>If you did not request this, you can ignore this email.</p>",
        };

    public static EmailMessage EmailVerification(string toEmail, string toName, string verifyUrl) =>
        new()
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = "Verify your BillFlow email",
            PlainTextBody =
                $"Hi {toName},\n\nVerify your email using this link (valid for 24 hours):\n{verifyUrl}\n\nThen you can sign in.",
            HtmlBody =
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(toName)},</p>"
                + "<p>Verify your BillFlow email using the link below (valid for 24 hours):</p>"
                + $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(verifyUrl)}\">Verify email</a></p>",
        };

    public static EmailMessage PaymentReminder(
        string toEmail,
        string toName,
        string companyName,
        string invoiceNumber,
        DateTime dueDate,
        decimal amountDue,
        string currency) =>
        new()
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = $"Reminder: invoice {invoiceNumber} from {companyName}",
            PlainTextBody =
                $"Hi {toName},\n\nThis is a friendly reminder that invoice {invoiceNumber} from {companyName} "
                + $"for {amountDue:0.00} {currency} is due on {dueDate:yyyy-MM-dd}.\n\nThank you.",
            HtmlBody =
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(toName)},</p>"
                + $"<p>This is a friendly reminder that invoice <strong>{System.Net.WebUtility.HtmlEncode(invoiceNumber)}</strong> "
                + $"from <strong>{System.Net.WebUtility.HtmlEncode(companyName)}</strong> "
                + $"for <strong>{amountDue:0.00} {System.Net.WebUtility.HtmlEncode(currency)}</strong> "
                + $"is due on <strong>{dueDate:yyyy-MM-dd}</strong>.</p>"
                + "<p>Thank you.</p>",
        };
}
