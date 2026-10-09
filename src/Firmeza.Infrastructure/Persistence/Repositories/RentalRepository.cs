using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Persistence.Repositories;

public class RentalRepository : IRentalRepository
{
    private readonly ApplicationDbContext _context;

    public RentalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rentals
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Rental>> GetAllAsync(Guid? customerId = null, RentalStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Rentals
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingRentalAsync(Guid vehicleId, DateTime startDate, DateTime endDate, Guid? excludeRentalId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Rentals.Where(r => r.VehicleId == vehicleId && r.Status != RentalStatus.Cancelled && r.Status != RentalStatus.Completed);

        if (excludeRentalId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRentalId.Value);
        }

        return await query.AnyAsync(r => r.StartDate < endDate && r.EndDate > startDate, cancellationToken);
    }

    public async Task<decimal> GetTotalRentalsRevenueAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rentals
            .Where(r => r.Status != RentalStatus.Cancelled)
            .SumAsync(r => r.Total, cancellationToken);
    }

    public async Task<int> GetActiveRentalsCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rentals
            .CountAsync(r => r.Status == RentalStatus.Active, cancellationToken);
    }

    public async Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        await _context.Rentals.AddAsync(rental, cancellationToken);
    }

    public void Update(Rental rental)
    {
        _context.Rentals.Update(rental);
    }
}
