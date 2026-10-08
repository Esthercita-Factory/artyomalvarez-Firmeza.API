using Firmeza.Application.Interfaces.Persistence;

namespace Firmeza.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly Data.ApplicationDbContext _context;

    public UnitOfWork(Data.ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
