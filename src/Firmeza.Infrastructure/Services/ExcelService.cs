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
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Omitir cabecera

        foreach (var row in rows)
        {
            var dto = new ExcelProductRowDto
            {
                Sku = row.Cell(1).GetString().Trim(),
                Name = row.Cell(2).GetString().Trim(),
                Brand = row.Cell(3).GetString().Trim(),
                Category = row.Cell(4).GetString().Trim(),
                UnitOfMeasure = row.Cell(5).IsEmpty() ? "unidad" : row.Cell(5).GetString().Trim(),
                UnitPrice = row.Cell(6).TryGetValue(out decimal price) ? price : 0m,
                Stock = row.Cell(7).TryGetValue(out int stock) ? stock : 0,
                MinimumStock = row.Cell(8).TryGetValue(out int minStock) ? minStock : 5,
                Description = row.Cell(9).GetString().Trim()
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
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var dto = new ExcelCustomerRowDto
            {
                FirstName = row.Cell(1).GetString().Trim(),
                LastName = row.Cell(2).GetString().Trim(),
                DocumentNumber = row.Cell(3).GetString().Trim(),
                DocumentType = row.Cell(4).IsEmpty() ? "CC" : row.Cell(4).GetString().Trim(),
                Email = row.Cell(5).GetString().Trim(),
                Phone = row.Cell(6).GetString().Trim(),
                Address = row.Cell(7).GetString().Trim(),
                Age = row.Cell(8).TryGetValue(out int age) ? age : null
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

        var productsToInsert = new List<Product>();

        foreach (var row in rows)
        {
            try
            {
                var existing = await _productRepository.GetBySkuAsync(row.Sku, cancellationToken);
                if (existing != null)
                {
                    existing.Name = row.Name;
                    existing.Brand = row.Brand;
                    existing.Category = row.Category;
                    existing.UnitPrice = row.UnitPrice;
                    existing.Stock += row.Stock;
                    existing.MinimumStock = row.MinimumStock;
                    _productRepository.Update(existing);
                }
                else
                {
                    productsToInsert.Add(new Product
                    {
                        Sku = row.Sku,
                        Name = row.Name,
                        Brand = row.Brand,
                        Category = row.Category,
                        UnitOfMeasure = row.UnitOfMeasure,
                        UnitPrice = row.UnitPrice,
                        Stock = row.Stock,
                        MinimumStock = row.MinimumStock,
                        Description = row.Description
                    });
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<ExcelImportResultDto> ImportCustomersFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var result = new ExcelImportResultDto();
        var rows = await ReadCustomersFromExcelAsync(fileStream, cancellationToken);
        result.TotalRows = rows.Count;

        var customersToInsert = new List<Customer>();

        foreach (var row in rows)
        {
            try
            {
                var existing = await _customerRepository.GetByDocumentAsync(row.DocumentNumber, cancellationToken);
                if (existing == null)
                {
                    customersToInsert.Add(new Customer
                    {
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        DocumentNumber = row.DocumentNumber,
                        DocumentType = row.DocumentType,
                        Email = row.Email,
                        Phone = row.Phone,
                        Address = row.Address,
                        Age = row.Age
                    });
                }
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
            await _customerRepository.AddRangeAsync(customersToInsert, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
}
