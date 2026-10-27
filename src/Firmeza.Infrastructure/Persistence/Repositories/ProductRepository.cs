using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Persistence;
using Firmeza.Infrastructure.Persistence.Extensions;
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
        var (items, _) = await GetPagedAsync(onlyActive, search, category, null, null, null, null, cancellationToken);
        return items;
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        bool onlyActive,
        string? search,
        string? category,
        string? status,
        bool? lowStock,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (lowStock == true)
        {
            query = query.WhereLowStock();
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var st = status.Trim().ToLower();
            if (st == "agotado" || st == "out_of_stock")
            {
                query = query.Where(p => p.Stock <= 0);
            }
            else if (st == "stock bajo" || st == "stock_bajo" || st == "low_stock" || st == "alerta")
            {
                query = query.WhereLowStock();
            }
            else if (st == "pocas unidades" || st == "pocas_unidades")
            {
                query = query.Where(p => p.Stock > 0 && p.Stock <= p.MinimumStock);
            }
            else if (st == "disponible" || st == "available")
            {
                query = query.Where(p => p.Stock > p.MinimumStock);
            }
            else if (st == "inactivo" || st == "inactive")
            {
                query = query.Where(p => !p.IsActive);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{term}%")
                                  || EF.Functions.ILike(p.Sku, $"%{term}%")
                                  || EF.Functions.ILike(p.Brand, $"%{term}%")
                                  || EF.Functions.ILike(p.Category, $"%{term}%")
                                  || (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var cat = category.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Category, $"%{cat}%") || EF.Functions.ILike(p.Sku, $"{cat}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderBy(p => p.Name);

        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .WhereLowStock()
            .OrderBy(p => p.Stock)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .WhereLowStock()
            .CountAsync(cancellationToken);
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
