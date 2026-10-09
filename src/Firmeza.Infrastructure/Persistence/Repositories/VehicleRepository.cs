using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Persistence.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Plate.ToLower() == plate.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(VehicleStatus? status = null, VehicleType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Vehicles.AsNoTracking().Where(v => v.IsActive);

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(v => v.Type == type.Value);
        }

        return await query.OrderBy(v => v.Brand).ThenBy(v => v.Model).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public void Update(Vehicle vehicle)
    {
        _context.Vehicles.Update(vehicle);
    }

    public void Delete(Vehicle vehicle)
    {
        vehicle.IsActive = false;
        _context.Vehicles.Update(vehicle);
    }
}
