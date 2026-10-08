using Firmeza.Domain.Common;
using Firmeza.Domain.Enums;

namespace Firmeza.Domain.Entities;

public class Rental : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public int DaysRented { get; set; }

    public decimal DailyRate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public RentalStatus Status { get; set; } = RentalStatus.Pending;

    public string? Notes { get; set; }
}
