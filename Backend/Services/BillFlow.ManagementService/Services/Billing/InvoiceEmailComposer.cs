using System.Net;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Shared.Email;

namespace BillFlow.ManagementService.Services.Billing;

public interface IInvoiceEmailComposer
{
    EmailMessage Compose(InvoiceDetailResponse invoice, CompanySettingsResponse? issuer, byte[] pdfContent);
}

public sealed class InvoiceEmailComposer : IInvoiceEmailComposer
{
    public EmailMessage Compose(InvoiceDetailResponse invoice, CompanySettingsResponse? issuer, byte[] pdfContent)
    {
        var companyName = issuer?.CompanyName ?? "BillFlow";
        var currency = issuer?.Currency ?? "USD";
        var subject = $"Invoice {invoice.InvoiceNumber} from {companyName}";

        var plain = $"""
            Hello {invoice.ClientContactName},

            Please find attached invoice {invoice.InvoiceNumber} from {companyName}.
            Total due: {invoice.Total:0.00} {currency}
            Due date: {invoice.DueDate:yyyy-MM-dd}

            Thank you for your business.
            """;

        var html = $"""
            <p>Hello {WebUtility.HtmlEncode(invoice.ClientContactName)},</p>
            <p>Please find attached invoice <strong>{WebUtility.HtmlEncode(invoice.InvoiceNumber)}</strong> from <strong>{WebUtility.HtmlEncode(companyName)}</strong>.</p>
            <p><strong>Total due:</strong> {invoice.Total:0.00} {WebUtility.HtmlEncode(currency)}<br/>
            <strong>Due date:</strong> {invoice.DueDate:yyyy-MM-dd}</p>
            <p>Thank you for your business.</p>
            """;

        return new EmailMessage
        {
            ToEmail = invoice.ClientEmail,
            ToName = invoice.ClientContactName,
            Subject = subject,
            HtmlBody = html,
            PlainTextBody = plain,
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = $"{invoice.InvoiceNumber}.pdf",
                    Content = pdfContent,
                    ContentType = "application/pdf",
                },
            ],
        };
    }
}
