using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Dashboard;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Interfaces.Services;

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, CancellationToken cancellationToken = default);
    Task<CartCalculationDto> CalculateCartAsync(List<CartItemDto> items, decimal taxRate = 0.19m, CancellationToken cancellationToken = default);
    Task<SaleDto> GetSaleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaleDto>> GetAllSalesAsync(Guid? customerId = null, CancellationToken cancellationToken = default);
}

public interface IRentalService
{
    Task<RentalDto> CreateRentalAsync(CreateRentalDto dto, CancellationToken cancellationToken = default);
    Task<RentalQuoteDto> CalculateRentalQuoteAsync(Guid vehicleId, DateTime startDate, DateTime endDate, decimal taxRate = 0.19m, CancellationToken cancellationToken = default);
    Task<RentalDto> GetRentalByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RentalDto>> GetAllRentalsAsync(Guid? customerId = null, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
}
