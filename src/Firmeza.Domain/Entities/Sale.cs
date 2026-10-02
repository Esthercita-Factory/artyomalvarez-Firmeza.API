namespace Firmeza.Domain.Entities;
public class Sale
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pendiente";
    public string? ExternalReference { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
}