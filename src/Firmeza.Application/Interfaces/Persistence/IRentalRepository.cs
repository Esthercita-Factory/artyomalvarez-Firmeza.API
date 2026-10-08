using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;

namespace Firmeza.Application.Interfaces.Persistence;

public interface IRentalRepository
{
    Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rental>> GetAllAsync(Guid? customerId = null, RentalStatus? status = null, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingRentalAsync(Guid vehicleId, DateTime startDate, DateTime endDate, Guid? excludeRentalId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRentalsRevenueAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveRentalsCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Rental rental, CancellationToken cancellationToken = default);
    void Update(Rental rental);
}
