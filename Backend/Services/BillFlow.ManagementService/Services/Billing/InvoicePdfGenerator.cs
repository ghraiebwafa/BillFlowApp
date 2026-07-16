using BillFlow.Models.Dtos.Billing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillFlow.ManagementService.Services.Billing;

public sealed class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(InvoiceDetailResponse invoice, CompanySettingsResponse? issuer = null) =>
        Document.Create(container =>
        {
            var accent = PdfBrandPalette.AccentHex(issuer);
            var currency = issuer?.Currency ?? "USD";
            var companyName = issuer?.CompanyName ?? "BillFlow";

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Height(4).Background(accent);
                    header.Item().PaddingTop(16).Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            if (issuer?.LogoBytes is { Length: > 0 })
                            {
                                column.Item().Width(96).Height(48).Image(issuer.LogoBytes).FitArea();
                                column.Item().PaddingTop(6);
                            }

                            column.Item().Text(companyName).FontSize(16).Bold().FontColor(accent);
                            if (!string.IsNullOrWhiteSpace(issuer?.Address))
                                column.Item().Text(issuer.Address);
                            if (!string.IsNullOrWhiteSpace(issuer?.Country))
                                column.Item().Text(issuer.Country);
                            if (!string.IsNullOrWhiteSpace(issuer?.Email))
                                column.Item().Text(issuer.Email);
                            if (!string.IsNullOrWhiteSpace(issuer?.PhoneNumber))
                                column.Item().Text(issuer.PhoneNumber);
                            if (!string.IsNullOrWhiteSpace(issuer?.TaxNumber))
                                column.Item().Text($"Tax ID: {issuer.TaxNumber}");
                        });

                        row.RelativeItem().Column(column =>
                        {
                            column.Item().AlignRight().Text("INVOICE").FontSize(24).Bold().FontColor(accent);
                            column.Item().AlignRight().Text($"#{invoice.InvoiceNumber}").FontSize(12);
                            column.Item().AlignRight().PaddingTop(8).Text($"Status: {invoice.Status}");
                        });
                    });
                });

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Bill to").Bold().FontColor(accent);
                            left.Item().Text(invoice.ClientCompanyName);
                            left.Item().Text(invoice.ClientContactName);
                            if (!string.IsNullOrWhiteSpace(invoice.ClientEmail))
                                left.Item().Text(invoice.ClientEmail);
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().AlignRight().Text($"Issue date: {invoice.InvoiceDate:yyyy-MM-dd}");
                            right.Item().AlignRight().Text($"Due date: {invoice.DueDate:yyyy-MM-dd}");
                            if (issuer is not null)
                                right.Item().AlignRight().Text($"Payment terms: {issuer.PaymentTermsDays} days");
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(accent).Padding(6).Text("Description").Bold().FontColor(Colors.White);
                            header.Cell().Background(accent).Padding(6).AlignRight().Text("Qty").Bold().FontColor(Colors.White);
                            header.Cell().Background(accent).Padding(6).AlignRight().Text($"Unit ({currency})").Bold().FontColor(Colors.White);
                            header.Cell().Background(accent).Padding(6).AlignRight().Text($"Total ({currency})").Bold().FontColor(Colors.White);
                        });

                        foreach (var line in invoice.LineItems.OrderBy(l => l.SortOrder))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(line.Description);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                                .Text(line.Quantity.ToString("0.##"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                                .Text(line.UnitPrice.ToString("0.00"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                                .Text(line.LineTotal.ToString("0.00"));
                        }
                    });

                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: {invoice.Subtotal:0.00} {currency}");
                        totals.Item().Text($"Tax ({invoice.TaxRate:0.##}%): {invoice.TaxAmount:0.00} {currency}");
                        totals.Item().Text($"Total: {invoice.Total:0.00} {currency}").Bold().FontSize(12).FontColor(accent);
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        column.Item().PaddingTop(12).Column(notes =>
                        {
                            notes.Item().Text("Notes").Bold().FontColor(accent);
                            notes.Item().Text(invoice.Notes);
                        });
                    }
                });

                page.Footer().Column(footer =>
                {
                    if (!string.IsNullOrWhiteSpace(issuer?.InvoiceFooterNote))
                        footer.Item().AlignCenter().Text(issuer.InvoiceFooterNote).FontSize(9);

                    footer.Item().AlignCenter().Text(text =>
                    {
                        text.Span("Generated by ");
                        text.Span(companyName).Bold().FontColor(accent);
                    });
                });
            });
        }).GeneratePdf();
}
