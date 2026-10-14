using AutoMapper;
using Firmeza.Application.Common.Exceptions;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Application.Services;
using Firmeza.Domain.Entities;
using Moq;
using Xunit;

namespace Firmeza.Tests;

public class SaleServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IPdfService> _pdfServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _sut = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _uowMock.Object,
            _mapperMock.Object,
            _pdfServiceMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task CalculateCartAsync_ShouldCalculateSubtotalTaxAndTotal_Correctly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Cemento Gris Argos 50kg",
            Sku = "ARG-CEM-50KG",
            UnitPrice = 30000m,
            Stock = 100
        };

        _productRepoMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var items = new List<CartItemDto>
        {
            new() { ProductId = productId, Quantity = 2 }
        };

        // Act
        var result = await _sut.CalculateCartAsync(items, 0.19m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60000m, result.Subtotal); // 30,000 * 2
        Assert.Equal(11400m, result.TaxAmount); // 60,000 * 0.19
        Assert.Equal(71400m, result.Total); // 60,000 + 11,400
        Assert.Single(result.Items);
        Assert.Equal("Cemento Gris Argos 50kg", result.Items[0].ProductName);
    }

    [Fact]
    public async Task CalculateCartAsync_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _productRepoMock
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var items = new List<CartItemDto>
        {
            new() { ProductId = nonExistentId, Quantity = 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CalculateCartAsync(items, 0.19m));
    }

    [Fact]
    public async Task CreateSaleAsync_WhenStockIsInsufficient_ShouldThrowValidationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = customerId,
            FirstName = "Pedro",
            LastName = "Pérez",
            Email = "pedro@test.com"
        };

        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Varilla Corrugada 1/2",
            Sku = "VAR-CORR-12",
            UnitPrice = 25000m,
            Stock = 5 // Solo 5 en inventario
        };

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepoMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var dto = new CreateSaleDto
        {
            CustomerId = customerId,
            Items = new List<CartItemDto>
            {
                new() { ProductId = productId, Quantity = 10 } // Pide 10
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateSaleAsync(dto));
        Assert.Contains("Stock insuficiente", ex.Message);
    }
}
