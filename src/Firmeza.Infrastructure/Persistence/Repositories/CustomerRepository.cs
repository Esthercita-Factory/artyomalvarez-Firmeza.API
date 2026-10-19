using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<Customer?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.DocumentNumber == documentNumber, cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return GetAllAsync(null, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.FirstName.ToLower().Contains(term)
                                  || c.LastName.ToLower().Contains(term)
                                  || c.DocumentNumber.ToLower().Contains(term)
                                  || c.Email.ToLower().Contains(term));
        }

        return await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(cancellationToken);
    }

    public async Task<Customer> GetOrCreateForUserAsync(string userId, string email, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (customer != null) return customer;

        customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
        if (customer != null)
        {
            if (string.IsNullOrEmpty(customer.UserId))
            {
                customer.UserId = userId;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return customer;
        }

        var docNum = "CLI-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        customer = new Customer
        {
            UserId = userId,
            Email = email,
            FirstName = email.Split('@')[0],
            LastName = "Cliente",
            DocumentType = "CC",
            DocumentNumber = docNum,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Customer> customers, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddRangeAsync(customers, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public void Delete(Customer customer)
    {
        _context.Customers.Remove(customer);
    }
}
