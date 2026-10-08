using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Infrastructure.Services;

public class PdfService : IPdfService
{
    public PdfService()
    {
        // Licencia comunitaria gratuita de QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateSaleReceiptPdfAsync(SaleDto sale, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // Encabezado
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FIRMEZA").FontSize(22).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("Materiales de Construcción y Alquiler de Vehículos").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().Text("NIT: 900.123.456-7 | Bogotá, Colombia").FontSize(9);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("COMPROBANTE DE VENTA").FontSize(14).Bold();
                        col.Item().Text($"Ref: {sale.Id.ToString()[..8].ToUpper()}").FontSize(10).Bold();
                        col.Item().Text($"Fecha: {sale.CreatedAtUtc:dd/MM/yyyy HH:mm} UTC").FontSize(9);
                        col.Item().Text($"Estado: {sale.Status}").FontSize(9).FontColor(Colors.Green.Darken2);
                    });
                });

                // Contenido
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    // Datos del Cliente
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CLIENTE").Bold().FontSize(9);
                            c.Item().Text(string.IsNullOrWhiteSpace(sale.CustomerName) ? "Consumidor Final" : sale.CustomerName);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("EMAIL").Bold().FontSize(9);
                            c.Item().Text(string.IsNullOrWhiteSpace(sale.CustomerEmail) ? "No especificado" : sale.CustomerEmail);
                        });
                    });

                    col.Item().PaddingTop(15);

                    // Tabla de Productos
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80); // SKU
                            columns.RelativeColumn(3);  // Nombre
                            columns.RelativeColumn(1);  // Cantidad
                            columns.RelativeColumn(1);  // Precio Unitario
                            columns.RelativeColumn(1);  // Total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("SKU").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Descripción").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Cant.").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Precio").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Total").FontColor(Colors.White).Bold();
                        });

                        foreach (var item in sale.Details)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ProductSku);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ProductName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Quantity.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"${item.UnitPrice:N2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"${item.LineTotal:N2}");
                        }
                    });

                    // Totales
                    col.Item().PaddingTop(15).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: ${sale.Subtotal:N2}").FontSize(10);
                        totals.Item().Text($"IVA ({(sale.TaxRate * 100):0}%): ${sale.TaxAmount:N2}").FontSize(10);
                        totals.Item().Text($"TOTAL: ${sale.Total:N2}").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                    });
                });

                // Pie de página
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Gracias por confiar en Firmeza. Documento generado automáticamente por el sistema.");
                    t.Span(" | Página ");
                    t.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }
}
