using Firmeza.Domain.Common;
using Firmeza.Domain.Enums;

namespace Firmeza.Domain.Entities;

public class Vehicle : BaseEntity
{
    public string Plate { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public VehicleType Type { get; set; }

    public decimal DailyRate { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    public string LoadCapacity { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
}
