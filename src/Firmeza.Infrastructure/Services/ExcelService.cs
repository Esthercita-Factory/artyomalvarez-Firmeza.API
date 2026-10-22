using ClosedXML.Excel;
using Firmeza.Application.DTOs.Excel;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;

namespace Firmeza.Infrastructure.Services;

public class ExcelService : IExcelService
{
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcelService(
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<ExcelProductRowDto>> ReadProductsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var list = new List<ExcelProductRowDto>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        if (range is null) return Task.FromResult<IReadOnlyList<ExcelProductRowDto>>(list);

        var firstRow = range.FirstRow();
        int lastCol = range.LastColumn().ColumnNumber();

        int colSku = -1, colName = -1, colBrand = -1, colCat = -1, colUnit = -1;
        int colPrice = -1, colStock = -1, colMinStock = -1, colDesc = -1;

        for (int c = 1; c <= lastCol; c++)
        {
            var header = firstRow.Cell(c).GetString().Trim().ToLowerInvariant();
            if (header == "sku" || header.Contains("código") || header.Contains("codigo")) colSku = c;
            else if (header.Contains("nombre") || header == "name") colName = c;
            else if (header.Contains("marca") || header == "brand") colBrand = c;
            else if (header.Contains("categor") || header == "category") colCat = c;
            else if (header.Contains("unidad") || header == "unit") colUnit = c;
            else if (header.Contains("precio") || header.Contains("price")) colPrice = c;
            else if (header.Contains("mín") || header.Contains("min")) colMinStock = c;
            else if (header.Contains("stock") || header.Contains("cant")) colStock = c;
            else if (header.Contains("desc")) colDesc = c;
        }

        // Posiciones por defecto si no hay cabeceras reconocibles
        if (colSku == -1 && colName == -1)
        {
            colSku = 1; colName = 2; colBrand = 3; colCat = 4; colUnit = 5;
            colPrice = 6; colStock = 7; colMinStock = 8; colDesc = 9;
        }

        var rows = range.RowsUsed().Skip(1); // Omitir cabecera

        foreach (var row in rows)
        {
            var sku = colSku > 0 ? row.Cell(colSku).GetString().Trim() : string.Empty;
            var name = colName > 0 ? row.Cell(colName).GetString().Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(name))
                continue;

            var dto = new ExcelProductRowDto
            {
                Sku = sku,
                Name = name,
                Brand = colBrand > 0 ? row.Cell(colBrand).GetString().Trim() : string.Empty,
                Category = colCat > 0 ? row.Cell(colCat).GetString().Trim() : "General",
                UnitOfMeasure = colUnit > 0 && !row.Cell(colUnit).IsEmpty() ? row.Cell(colUnit).GetString().Trim() : "unidad",
                UnitPrice = (colPrice > 0 && row.Cell(colPrice).TryGetValue(out decimal price)) ? price : 0m,
                Stock = (colStock > 0 && row.Cell(colStock).TryGetValue(out int stock)) ? stock : 0,
                MinimumStock = (colMinStock > 0 && row.Cell(colMinStock).TryGetValue(out int minStock)) ? minStock : 5,
                Description = colDesc > 0 ? row.Cell(colDesc).GetString().Trim() : string.Empty
            };

            if (!string.IsNullOrWhiteSpace(dto.Sku) && !string.IsNullOrWhiteSpace(dto.Name))
            {
                list.Add(dto);
            }
        }

        return Task.FromResult<IReadOnlyList<ExcelProductRowDto>>(list);
    }

    public Task<IReadOnlyList<ExcelCustomerRowDto>> ReadCustomersFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var list = new List<ExcelCustomerRowDto>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        if (range is null) return Task.FromResult<IReadOnlyList<ExcelCustomerRowDto>>(list);

        var firstRow = range.FirstRow();
        int lastCol = range.LastColumn().ColumnNumber();

        // Mapeo dinámico e inteligente de cabeceras
        int colFirstName = -1, colLastName = -1, colDoc = -1, colDocType = -1;
        int colEmail = -1, colPhone = -1, colAddress = -1, colAge = -1, colFullName = -1;

        for (int c = 1; c <= lastCol; c++)
        {
            var header = firstRow.Cell(c).GetString().Trim().ToLowerInvariant();
            if (header.Contains("documento") || header.Contains("cédula") || header.Contains("cedula") || header.Contains("doc") || header.Contains("identidad"))
            {
                if (header.Contains("tipo")) colDocType = c;
                else colDoc = c;
            }
            else if (header.Contains("tipo") && (header.Contains("doc") || header.Contains("ident")))
            {
                colDocType = c;
            }
            else if (header.Contains("correo") || header.Contains("email") || header.Contains("mail"))
            {
                colEmail = c;
            }
            else if (header.Contains("completo"))
            {
                colFullName = c;
            }
            else if (header.Contains("nombre") || header.Contains("first"))
            {
                colFirstName = c;
            }
            else if (header.Contains("apellido") || header.Contains("last"))
            {
                colLastName = c;
            }
            else if (header.Contains("tel") || header.Contains("cel") || header.Contains("phone"))
            {
                colPhone = c;
            }
            else if (header.Contains("dir") || header.Contains("address"))
            {
                colAddress = c;
            }
            else if (header.Contains("edad") || header.Contains("age"))
            {
                colAge = c;
            }
        }

        // Si no se encontraron cabeceras reconocibles, usar posiciones por defecto
        if (colDoc == -1 && colEmail == -1)
        {
            colFirstName = 1;
            colLastName = 2;
            colDoc = 3;
            colDocType = 4;
            colEmail = 5;
            colPhone = 6;
            colAddress = 7;
            colAge = 8;
        }

        var dataRows = range.RowsUsed().Skip(1);

        foreach (var row in dataRows)
        {
            string doc = colDoc > 0 ? row.Cell(colDoc).GetString().Trim() : string.Empty;
            string email = colEmail > 0 ? row.Cell(colEmail).GetString().Trim() : string.Empty;

            // Omitir filas vacías
            if (string.IsNullOrWhiteSpace(doc) && string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            string firstName = colFirstName > 0 ? row.Cell(colFirstName).GetString().Trim() : string.Empty;
            string lastName = colLastName > 0 ? row.Cell(colLastName).GetString().Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(firstName) && colFullName > 0)
            {
                var full = row.Cell(colFullName).GetString().Trim();
                var parts = full.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = parts.Length > 0 ? parts[0] : full;
                lastName = parts.Length > 1 ? parts[1] : string.Empty;
            }

            string docType = colDocType > 0 && !row.Cell(colDocType).IsEmpty()
                ? row.Cell(colDocType).GetString().Trim()
                : "CC";

            string? phone = colPhone > 0 ? row.Cell(colPhone).GetString().Trim() : null;
            string? address = colAddress > 0 ? row.Cell(colAddress).GetString().Trim() : null;
            int? age = (colAge > 0 && row.Cell(colAge).TryGetValue(out int a)) ? a : null;

            var dto = new ExcelCustomerRowDto
            {
                FirstName = firstName,
                LastName = lastName,
                DocumentNumber = doc,
                DocumentType = docType,
                Email = email,
                Phone = phone,
                Address = address,
                Age = age
            };

            if (!string.IsNullOrWhiteSpace(dto.DocumentNumber) && !string.IsNullOrWhiteSpace(dto.Email))
            {
                list.Add(dto);
            }
        }

        return Task.FromResult<IReadOnlyList<ExcelCustomerRowDto>>(list);
    }

    public async Task<ExcelImportResultDto> ImportProductsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var result = new ExcelImportResultDto();
        var rows = await ReadProductsFromExcelAsync(fileStream, cancellationToken);
        result.TotalRows = rows.Count;

        // Cargar productos existentes en una sola consulta para optimizar rendimiento
        var existingProducts = await _productRepository.GetAllAsync(false, null, null, cancellationToken);
        var productDict = existingProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
            .ToDictionary(p => p.Sku.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

        var productsToInsert = new List<Product>();

        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Sku))
                {
                    result.ErrorCount++;
                    result.Errors.Add("Fila omitida: El SKU del producto es obligatorio.");
                    continue;
                }

                var skuKey = row.Sku.Trim().ToUpperInvariant();
                if (productDict.TryGetValue(skuKey, out var existing))
                {
                    existing.Name = string.IsNullOrWhiteSpace(row.Name) ? existing.Name : row.Name.Trim();
                    existing.Brand = string.IsNullOrWhiteSpace(row.Brand) ? existing.Brand : row.Brand.Trim();
                    existing.Category = string.IsNullOrWhiteSpace(row.Category) ? (existing.Category ?? "General") : row.Category.Trim();
                    existing.UnitPrice = row.UnitPrice > 0 ? row.UnitPrice : existing.UnitPrice;
                    existing.Stock += row.Stock;
                    existing.MinimumStock = row.MinimumStock >= 0 ? row.MinimumStock : existing.MinimumStock;
                    _productRepository.Update(existing);
                }
                else
                {
                    var newProduct = new Product
                    {
                        Sku = skuKey,
                        Name = row.Name?.Trim() ?? string.Empty,
                        Brand = row.Brand?.Trim() ?? string.Empty,
                        Category = string.IsNullOrWhiteSpace(row.Category) ? "General" : row.Category.Trim(),
                        UnitOfMeasure = string.IsNullOrWhiteSpace(row.UnitOfMeasure) ? "unidad" : row.UnitOfMeasure.Trim(),
                        UnitPrice = row.UnitPrice,
                        Stock = row.Stock,
                        MinimumStock = row.MinimumStock,
                        Description = row.Description?.Trim()
                    };
                    productsToInsert.Add(newProduct);
                    productDict[skuKey] = newProduct;
                }
                result.ProcessedCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Error en SKU {row.Sku}: {ex.Message}");
            }
        }

        if (productsToInsert.Count > 0)
        {
            await _productRepository.AddRangeAsync(productsToInsert, cancellationToken);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            result.ErrorCount += productsToInsert.Count;
            result.ProcessedCount -= productsToInsert.Count;
            result.Errors.Add($"Error al guardar en base de datos: {ex.Message}");
        }

        return result;
    }

    public async Task<ExcelImportResultDto> ImportCustomersFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var result = new ExcelImportResultDto();
        var rows = await ReadCustomersFromExcelAsync(fileStream, cancellationToken);
        result.TotalRows = rows.Count;

        // Cargar clientes existentes en una sola consulta para evitar latencias de 7-8 segundos por N llamadas
        var existingCustomers = await _customerRepository.GetAllAsync(cancellationToken);
        var existingDocs = existingCustomers.Where(c => !string.IsNullOrWhiteSpace(c.DocumentNumber))
                                            .Select(c => c.DocumentNumber.Trim())
                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingEmails = existingCustomers.Where(c => !string.IsNullOrWhiteSpace(c.Email))
                                              .Select(c => c.Email.Trim())
                                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenDocs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var customersToInsert = new List<Customer>();

        foreach (var row in rows)
        {
            try
            {
                var doc = row.DocumentNumber?.Trim();
                var email = row.Email?.Trim();

                if (string.IsNullOrWhiteSpace(doc))
                {
                    result.ErrorCount++;
                    result.Errors.Add("Fila omitida: El número de documento es obligatorio.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Fila {doc}: El correo electrónico es obligatorio.");
                    continue;
                }

                if (existingDocs.Contains(doc) || seenDocs.Contains(doc))
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Fila omitida: Ya existe un cliente con el documento '{doc}'.");
                    continue;
                }

                if (existingEmails.Contains(email) || seenEmails.Contains(email))
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Fila omitida: El correo '{email}' ya se encuentra registrado.");
                    continue;
                }

                seenDocs.Add(doc);
                seenEmails.Add(email);

                customersToInsert.Add(new Customer
                {
                    FirstName = row.FirstName?.Trim() ?? string.Empty,
                    LastName = row.LastName?.Trim() ?? string.Empty,
                    DocumentNumber = doc,
                    DocumentType = string.IsNullOrWhiteSpace(row.DocumentType) ? "CC" : row.DocumentType.Trim(),
                    Email = email,
                    Phone = row.Phone?.Trim(),
                    Address = row.Address?.Trim(),
                    Age = row.Age
                });

                result.ProcessedCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Error en Documento {row.DocumentNumber}: {ex.Message}");
            }
        }

        if (customersToInsert.Count > 0)
        {
            try
            {
                await _customerRepository.AddRangeAsync(customersToInsert, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                result.ErrorCount += customersToInsert.Count;
                result.ProcessedCount -= customersToInsert.Count;
                result.Errors.Add($"Error de base de datos al guardar los clientes: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<byte[]> ExportSalesToExcelAsync(CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.GetAllAsync(null, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ventas Firmeza");

        // Cabeceras
        worksheet.Cell(1, 1).Value = "ID Venta";
        worksheet.Cell(1, 2).Value = "Fecha (UTC)";
        worksheet.Cell(1, 3).Value = "Cliente";
        worksheet.Cell(1, 4).Value = "Estado";
        worksheet.Cell(1, 5).Value = "Subtotal";
        worksheet.Cell(1, 6).Value = "IVA (Impuesto)";
        worksheet.Cell(1, 7).Value = "Total";
        worksheet.Row(1).Style.Font.Bold = true;

        int rowIdx = 2;
        foreach (var sale in sales)
        {
            worksheet.Cell(rowIdx, 1).Value = sale.Id.ToString();
            worksheet.Cell(rowIdx, 2).Value = sale.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(rowIdx, 3).Value = sale.Customer != null ? $"{sale.Customer.FirstName} {sale.Customer.LastName}" : "Consumidor Final";
            worksheet.Cell(rowIdx, 4).Value = sale.Status;
            worksheet.Cell(rowIdx, 5).Value = sale.Subtotal;
            worksheet.Cell(rowIdx, 6).Value = sale.TaxAmount;
            worksheet.Cell(rowIdx, 7).Value = sale.Total;
            rowIdx++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportProductsToExcelAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(false, null, null, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Productos Firmeza");

        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "SKU";
        worksheet.Cell(1, 3).Value = "Nombre";
        worksheet.Cell(1, 4).Value = "Marca";
        worksheet.Cell(1, 5).Value = "Categoría";
        worksheet.Cell(1, 6).Value = "Unidad";
        worksheet.Cell(1, 7).Value = "Precio Unitario";
        worksheet.Cell(1, 8).Value = "Stock";
        worksheet.Cell(1, 9).Value = "Stock Mínimo";
        worksheet.Cell(1, 10).Value = "Activo";
        worksheet.Row(1).Style.Font.Bold = true;

        int rowIdx = 2;
        foreach (var p in products)
        {
            worksheet.Cell(rowIdx, 1).Value = p.Id.ToString();
            worksheet.Cell(rowIdx, 2).Value = p.Sku;
            worksheet.Cell(rowIdx, 3).Value = p.Name;
            worksheet.Cell(rowIdx, 4).Value = p.Brand;
            worksheet.Cell(rowIdx, 5).Value = p.Category;
            worksheet.Cell(rowIdx, 6).Value = p.UnitOfMeasure;
            worksheet.Cell(rowIdx, 7).Value = p.UnitPrice;
            worksheet.Cell(rowIdx, 8).Value = p.Stock;
            worksheet.Cell(rowIdx, 9).Value = p.MinimumStock;
            worksheet.Cell(rowIdx, 10).Value = p.IsActive ? "Sí" : "No";
            rowIdx++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportCustomersToExcelAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(null, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clientes Firmeza");

        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Nombre Completo";
        worksheet.Cell(1, 3).Value = "Tipo Doc";
        worksheet.Cell(1, 4).Value = "Número Doc";
        worksheet.Cell(1, 5).Value = "Correo";
        worksheet.Cell(1, 6).Value = "Teléfono";
        worksheet.Cell(1, 7).Value = "Dirección";
        worksheet.Cell(1, 8).Value = "Edad";
        worksheet.Row(1).Style.Font.Bold = true;

        int rowIdx = 2;
        foreach (var c in customers)
        {
            worksheet.Cell(rowIdx, 1).Value = c.Id.ToString();
            worksheet.Cell(rowIdx, 2).Value = $"{c.FirstName} {c.LastName}".Trim();
            worksheet.Cell(rowIdx, 3).Value = c.DocumentType;
            worksheet.Cell(rowIdx, 4).Value = c.DocumentNumber;
            worksheet.Cell(rowIdx, 5).Value = c.Email;
            worksheet.Cell(rowIdx, 6).Value = c.Phone ?? string.Empty;
            worksheet.Cell(rowIdx, 7).Value = c.Address ?? string.Empty;
            worksheet.Cell(rowIdx, 8).Value = c.Age.HasValue ? c.Age.Value.ToString() : string.Empty;
            rowIdx++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
