using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Interfaces.Services;

public interface IPdfService
{
    Task<byte[]> GenerateSaleReceiptPdfAsync(SaleDto sale, CancellationToken cancellationToken = default);
}
