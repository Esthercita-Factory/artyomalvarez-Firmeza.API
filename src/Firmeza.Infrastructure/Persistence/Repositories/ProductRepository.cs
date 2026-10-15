using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        return GetAllAsync(onlyActive, null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(bool onlyActive, string? search, string? category, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();
        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                                  || p.Sku.ToLower().Contains(term)
                                  || p.Brand.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == cat);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
            .OrderBy(p => p.Stock)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddRangeAsync(products, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public void Delete(Product product)
    {
        _context.Products.Remove(product);
    }
}
