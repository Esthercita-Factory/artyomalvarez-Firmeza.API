namespace Firmeza.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Regla única de negocio: Un producto está en stock bajo si está activo y su stock es menor o igual al mínimo.
    /// </summary>
    public bool IsLowStock => IsActive && Stock <= MinimumStock;

    /// <summary>
    /// Estado calculado dinámicamente según existencias.
    /// </summary>
    public string Status => !IsActive ? "Inactivo" : Stock <= 0 ? "Agotado" : Stock <= MinimumStock ? "Pocas unidades" : "Disponible";
}

public class CreateProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "unidad";
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
}

public class UpdateProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "unidad";
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
}
