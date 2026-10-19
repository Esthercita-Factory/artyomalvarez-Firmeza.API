using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Validators.Customers;
using Firmeza.Application.Validators.Sales;
using Xunit;

namespace Firmeza.Tests;

public class ValidatorTests
{
    private readonly CreateCustomerDtoValidator _customerValidator = new();
    private readonly CreateSaleDtoValidator _saleValidator = new();

    [Fact]
    public void CreateCustomerDtoValidator_WithInvalidEmail_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            DocumentNumber = "123456789",
            Email = "correo-invalido-sin-arroba",
            Age = 25
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCustomerDto.Email));
    }

    [Fact]
    public void CreateCustomerDtoValidator_WithUnderageCustomer_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            DocumentNumber = "123456789",
            Email = "juan@example.com",
            Age = 16 // Menor de edad
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCustomerDto.Age));
    }

    [Fact]
    public void CreateCustomerDtoValidator_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            DocumentNumber = "123456789",
            Email = "juan@example.com",
            Age = 30
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateCustomerDtoValidator_WithInvalidPhone_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            DocumentNumber = "123456789",
            Email = "juan@example.com",
            Phone = "abc-no-telefono",
            Age = 30
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCustomerDto.Phone));
    }

    [Fact]
    public void CreateCustomerDtoValidator_WithValidPhone_ShouldBeValid()
    {
        // Arrange
        var dto = new CreateCustomerDto
        {
            FirstName = "Juan",
            LastName = "Pérez",
            DocumentNumber = "123456789",
            Email = "juan@example.com",
            Phone = "+57 300 1234567",
            Age = 30
        };

        // Act
        var result = _customerValidator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateSaleDtoValidator_WithEmptyItemsList_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateSaleDto
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CartItemDto>() // Carrito vacío
        };

        // Act
        var result = _saleValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateSaleDto.Items));
    }
}
