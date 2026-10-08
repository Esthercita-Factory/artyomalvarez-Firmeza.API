using Firmeza.Domain.Enums;

namespace Firmeza.Application.DTOs.Vehicles;

public class VehicleDto
{
    public Guid Id { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleType Type { get; set; }
    public decimal DailyRate { get; set; }
    public VehicleStatus Status { get; set; }
    public string LoadCapacity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateVehicleDto
{
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleType Type { get; set; }
    public decimal DailyRate { get; set; }
    public string LoadCapacity { get; set; } = string.Empty;
}

public class UpdateVehicleDto
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleType Type { get; set; }
    public decimal DailyRate { get; set; }
    public VehicleStatus Status { get; set; }
    public string LoadCapacity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
