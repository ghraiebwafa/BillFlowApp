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
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                var currency = issuer?.Currency ?? "USD";

                page.Header().Row(row =>
                {
                    if (issuer is not null)
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text(issuer.CompanyName).FontSize(14).Bold();
                            if (!string.IsNullOrWhiteSpace(issuer.Address))
                                column.Item().Text(issuer.Address);
                            if (!string.IsNullOrWhiteSpace(issuer.Country))
                                column.Item().Text(issuer.Country);
                            if (!string.IsNullOrWhiteSpace(issuer.Email))
                                column.Item().Text(issuer.Email);
                            if (!string.IsNullOrWhiteSpace(issuer.PhoneNumber))
                                column.Item().Text(issuer.PhoneNumber);
                            if (!string.IsNullOrWhiteSpace(issuer.TaxNumber))
                                column.Item().Text($"Tax ID: {issuer.TaxNumber}");
                        });
                    }

                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignRight().Text("INVOICE").FontSize(22).Bold();
                        column.Item().AlignRight().Text($"#{invoice.InvoiceNumber}").FontSize(12);
                        column.Item().AlignRight().PaddingTop(8).Text($"Status: {invoice.Status}");
                    });
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Bill to").Bold();
                            left.Item().Text(invoice.ClientCompanyName);
                            left.Item().Text(invoice.ClientContactName);
                            left.Item().Text(invoice.ClientEmail);
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().AlignRight().Text($"Issue date: {invoice.InvoiceDate:yyyy-MM-dd}");
                            right.Item().AlignRight().Text($"Due date: {invoice.DueDate:yyyy-MM-dd}");
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
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Qty").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"Unit ({currency})").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"Total ({currency})").Bold();
                        });

                        foreach (var line in invoice.LineItems.OrderBy(l => l.SortOrder))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                .Text(line.Description);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text(line.Quantity.ToString("0.##"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text(line.UnitPrice.ToString("0.00"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text(line.LineTotal.ToString("0.00"));
                        }
                    });

                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: {invoice.Subtotal:0.00} {currency}");
                        totals.Item().Text($"Tax ({invoice.TaxRate:0.##}%): {invoice.TaxAmount:0.00} {currency}");
                        totals.Item().Text($"Total: {invoice.Total:0.00} {currency}").Bold().FontSize(12);
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        column.Item().PaddingTop(12).Column(notes =>
                        {
                            notes.Item().Text("Notes").Bold();
                            notes.Item().Text(invoice.Notes);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by ");
                    text.Span(issuer?.CompanyName ?? "BillFlow").Bold();
                });
            });
        }).GeneratePdf();
}
