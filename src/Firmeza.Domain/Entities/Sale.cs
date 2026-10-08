using Firmeza.Domain.Common;

namespace Firmeza.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public string Status { get; set; } = "Pendiente";

    public string? ExternalReference { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}