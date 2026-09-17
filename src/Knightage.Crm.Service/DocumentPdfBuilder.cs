using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Knightage.Crm.Service;

// QuestPDF.Fluent also defines a type named "Document" -- every reference to our own
// Document model below is fully qualified to avoid that ambiguity.
public class DocumentPdfBuilder : IDocumentPdfBuilder
{
    public byte[] Build(Knightage.Crm.Core.Models.Document document, IReadOnlyList<DocumentLineItem> lineItems, Lead lead, OrganizationDetails organization, byte[]? logoBytes = null)
    {
        var pdf = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        if (logoBytes is { Length: > 0 })
                        {
                            column.Item().MaxHeight(40).Image(logoBytes).FitHeight();
                        }
                        column.Item().Text(organization.Name).FontSize(16).Bold();
                        if (!string.IsNullOrWhiteSpace(organization.Address))
                        {
                            column.Item().Text(organization.Address!);
                        }
                        if (!string.IsNullOrWhiteSpace(organization.ContactEmail))
                        {
                            column.Item().Text(organization.ContactEmail!);
                        }
                        if (!string.IsNullOrWhiteSpace(organization.ContactPhone))
                        {
                            column.Item().Text(organization.ContactPhone!);
                        }
                    });
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignRight().Text(document.Type.ToUpperInvariant()).FontSize(16).Bold();
                        column.Item().AlignRight().Text(document.Number);
                        column.Item().AlignRight().Text(document.CreatedAtUtc.ToString("yyyy-MM-dd"));
                        column.Item().AlignRight().Text(document.Status);
                    });
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Text("Bill To").Bold();
                    column.Item().Text(lead.Name);
                    if (!string.IsNullOrWhiteSpace(lead.Company))
                    {
                        column.Item().Text(lead.Company!);
                    }
                    if (!string.IsNullOrWhiteSpace(lead.Email))
                    {
                        column.Item().Text(lead.Email!);
                    }
                    if (!string.IsNullOrWhiteSpace(lead.Phone))
                    {
                        column.Item().Text(lead.Phone!);
                    }

                    column.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product").Bold();
                            header.Cell().Text("Qty").Bold();
                            header.Cell().Text("Unit Price").Bold();
                            header.Cell().Text("Line Total").Bold();
                        });

                        foreach (var item in lineItems)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().Text(item.UnitPrice.ToString("N2"));
                            table.Cell().Text(item.LineTotal.ToString("N2"));
                        }
                    });

                    column.Item().PaddingTop(10).AlignRight().Text($"Subtotal: {document.Subtotal:N2}");
                    if (document.DiscountPercent > 0)
                    {
                        column.Item().AlignRight().Text($"Discount ({document.DiscountPercent:0.##}%): -{document.DiscountAmount:N2}");
                    }
                    column.Item().AlignRight().Text($"Tax ({document.TaxRatePercent:0.##}%): {document.TaxAmount:N2}");
                    column.Item().AlignRight().Text($"Total: {document.Total:N2}").FontSize(12).Bold();
                });
            });
        });

        return pdf.GeneratePdf();
    }
}
