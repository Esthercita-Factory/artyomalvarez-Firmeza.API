using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Products;
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

    public Task<byte[]> GenerateProductsCatalogPdfAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FIRMEZA — CATÁLOGO OFICIAL DE MATERIALES").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC | Total referencias: {products.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);  // SKU
                        columns.RelativeColumn(3);   // Nombre
                        columns.RelativeColumn(2);   // Marca
                        columns.RelativeColumn(2);   // Categoría
                        columns.RelativeColumn(1);   // Unidad
                        columns.RelativeColumn(2);   // Precio
                        columns.RelativeColumn(1);   // Stock
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("SKU").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Nombre").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Marca").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Categoría").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Unidad").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Precio").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Stock").FontColor(Colors.White).Bold();
                    });

                    foreach (var p in products)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Sku);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Name);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Brand);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Category);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.UnitOfMeasure);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"${p.UnitPrice:N2}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Stock.ToString());
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Firmeza S.A.S. — Reporte de Catálogo | Página ");
                    t.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    public Task<byte[]> GenerateCustomersReportPdfAsync(IReadOnlyList<CustomerDto> customers, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FIRMEZA — DIRECTORIO CORPORATIVO DE CLIENTES").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC | Total clientes: {customers.Count}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);  // Documento
                        columns.RelativeColumn(3);   // Nombre
                        columns.RelativeColumn(3);   // Correo
                        columns.RelativeColumn(2);   // Teléfono
                        columns.RelativeColumn(3);   // Dirección
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Doc. Identidad").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Nombre Completo").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Correo").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Teléfono").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Dirección").FontColor(Colors.White).Bold();
                    });

                    foreach (var c in customers)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{c.DocumentType} {c.DocumentNumber}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{c.FirstName} {c.LastName}".Trim());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.Email);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.Phone ?? "-");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.Address ?? "-");
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Firmeza S.A.S. — Directorio de Clientes | Página ");
                    t.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }
}
