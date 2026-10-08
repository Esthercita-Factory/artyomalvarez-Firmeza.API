using Firmeza.Domain.Entities;

namespace Firmeza.Application.Interfaces.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Customer> customers, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    void Delete(Customer customer);
}
