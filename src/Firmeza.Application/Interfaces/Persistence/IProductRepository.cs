using Firmeza.Domain.Entities;

namespace Firmeza.Application.Interfaces.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(bool onlyActive, string? search, string? category, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        bool onlyActive,
        string? search,
        string? category,
        string? status,
        bool? lowStock,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Delete(Product product);
}
