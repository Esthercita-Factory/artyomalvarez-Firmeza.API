using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Interfaces.Services;

public interface IPdfService
{
    Task<byte[]> GenerateSaleReceiptPdfAsync(SaleDto sale, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateProductsCatalogPdfAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCustomersReportPdfAsync(IReadOnlyList<CustomerDto> customers, CancellationToken cancellationToken = default);
}
