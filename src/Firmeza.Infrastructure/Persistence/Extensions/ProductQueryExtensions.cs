using Firmeza.Domain.Entities;

namespace Firmeza.Infrastructure.Persistence.Extensions;

public static class ProductQueryExtensions
{
    /// <summary>
    /// Expresión reutilizable en consultas EF Core para filtrar productos en stock bajo.
    /// Regla única institucional: IsActive && Stock <= MinimumStock.
    /// </summary>
    public static IQueryable<Product> WhereLowStock(this IQueryable<Product> query)
    {
        return query.Where(p => p.IsActive && p.Stock <= p.MinimumStock);
    }
}
