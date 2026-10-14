using AutoMapper;
using Firmeza.Application.Common.Exceptions;
using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Services;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Moq;
using Xunit;

namespace Firmeza.Tests;

public class RentalServiceTests
{
    private readonly Mock<IRentalRepository> _rentalRepoMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private readonly RentalService _sut;

    public RentalServiceTests()
    {
        _sut = new RentalService(
            _rentalRepoMock.Object,
            _vehicleRepoMock.Object,
            _customerRepoMock.Object,
            _uowMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task CalculateRentalQuoteAsync_ShouldCalculateDaysAndTotals_Correctly()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Brand = "Caterpillar",
            Model = "320D",
            DailyRate = 450000m,
            Status = VehicleStatus.Available
        };

        _vehicleRepoMock
            .Setup(r => r.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var startDate = new DateTime(2026, 11, 1);
        var endDate = new DateTime(2026, 11, 4); // 3 días

        // Act
        var quote = await _sut.CalculateRentalQuoteAsync(vehicleId, startDate, endDate, 0.19m);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal(3, quote.DaysRented);
        Assert.Equal(1350000m, quote.Subtotal); // 450,000 * 3
        Assert.Equal(256500m, quote.TaxAmount); // 1,350,000 * 0.19
        Assert.Equal(1606500m, quote.Total); // 1,350,000 + 256,500
    }

    [Fact]
    public async Task CreateRentalAsync_WhenVehicleIsNotAvailable_ShouldThrowValidationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer { Id = customerId, FirstName = "Ana", LastName = "Gómez" };
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Brand = "Chevrolet",
            Model = "NHR",
            Status = VehicleStatus.Maintenance // En mantenimiento, no disponible
        };

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepoMock
            .Setup(r => r.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var dto = new CreateRentalDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateRentalAsync(dto));
        Assert.Contains("no se encuentra disponible", ex.Message);
    }

    [Fact]
    public async Task CreateRentalAsync_WhenDatesOverlap_ShouldThrowValidationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer { Id = customerId, FirstName = "Carlos", LastName = "Ruiz" };
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Brand = "Komatsu",
            Model = "PC200",
            DailyRate = 500000m,
            Status = VehicleStatus.Available
        };

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepoMock
            .Setup(r => r.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _rentalRepoMock
            .Setup(r => r.HasOverlappingRentalAsync(vehicleId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Ya existe solapamiento

        var dto = new CreateRentalDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateRentalAsync(dto));
        Assert.Contains("ya tiene una reserva en el rango de fechas", ex.Message);
    }
}
