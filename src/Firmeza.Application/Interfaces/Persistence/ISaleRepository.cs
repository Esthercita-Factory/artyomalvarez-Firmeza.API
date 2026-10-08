using Firmeza.Domain.Entities;

namespace Firmeza.Application.Interfaces.Persistence;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetAllAsync(Guid? customerId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSalesRevenueAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalSalesCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
    void Update(Sale sale);
}
