using Firmeza.Application.DTOs.Excel;

namespace Firmeza.Application.Interfaces.Services;

public interface IExcelService
{
    Task<IReadOnlyList<ExcelProductRowDto>> ReadProductsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExcelCustomerRowDto>> ReadCustomersFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
    Task<ExcelImportResultDto> ImportProductsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
    Task<ExcelImportResultDto> ImportCustomersFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
    Task<byte[]> ExportSalesToExcelAsync(CancellationToken cancellationToken = default);
}
