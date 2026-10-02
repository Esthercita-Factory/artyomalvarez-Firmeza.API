namespace Firmeza.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = "unidad";

    public decimal UnitPrice { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}