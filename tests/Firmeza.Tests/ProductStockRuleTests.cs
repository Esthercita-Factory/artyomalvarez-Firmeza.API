using Firmeza.Application.DTOs.Products;
using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Persistence.Extensions;
using Xunit;

namespace Firmeza.Tests;

public class ProductStockRuleTests
{
    [Fact]
    public void Product_WhenStockEqualsMinimum_IsLowStock_ReturnsTrue()
    {
        // Arrange: Stock == MinimumStock (Umbral exacto)
        var product = new Product
        {
            Stock = 10,
            MinimumStock = 10,
            IsActive = true
        };

        // Act & Assert
        Assert.True(product.IsLowStock);
    }

    [Fact]
    public void Product_WhenStockLessThanMinimum_IsLowStock_ReturnsTrue()
    {
        // Arrange: Stock < MinimumStock
        var product = new Product
        {
            Stock = 4,
            MinimumStock = 10,
            IsActive = true
        };

        // Act & Assert
        Assert.True(product.IsLowStock);
    }

    [Fact]
    public void Product_WhenStockGreaterThanMinimum_IsLowStock_ReturnsFalse()
    {
        // Arrange: Stock > MinimumStock (Inventario saludable)
        var product = new Product
        {
            Stock = 11,
            MinimumStock = 10,
            IsActive = true
        };

        // Act & Assert
        Assert.False(product.IsLowStock);
    }

    [Fact]
    public void Product_WhenStockIsZero_IsLowStock_ReturnsTrue()
    {
        // Arrange: Stock == 0 (Agotado crítico)
        var product = new Product
        {
            Stock = 0,
            MinimumStock = 10,
            IsActive = true
        };

        // Act & Assert
        Assert.True(product.IsLowStock);
    }

    [Fact]
    public void Product_WhenInactive_IsLowStock_ReturnsFalse()
    {
        // Arrange: Inactivo no debe disparar alarmas operativas
        var product = new Product
        {
            Stock = 2,
            MinimumStock = 10,
            IsActive = false
        };

        // Act & Assert
        Assert.False(product.IsLowStock);
    }

    [Theory]
    [InlineData(0, 10, true, "Agotado", true)]
    [InlineData(5, 10, true, "Pocas unidades", true)]
    [InlineData(10, 10, true, "Pocas unidades", true)]
    [InlineData(11, 10, true, "Disponible", false)]
    [InlineData(5, 10, false, "Inactivo", false)]
    public void ProductDto_CalculatesStatusAndLowStock_Consistently(
        int stock, int minimumStock, bool isActive, string expectedStatus, bool expectedLowStock)
    {
        // Arrange
        var dto = new ProductDto
        {
            Stock = stock,
            MinimumStock = minimumStock,
            IsActive = isActive
        };

        // Act & Assert
        Assert.Equal(expectedStatus, dto.Status);
        Assert.Equal(expectedLowStock, dto.IsLowStock);
    }

    [Fact]
    public void ProductQueryExtensions_WhereLowStock_FiltersMemoryQueryableCorrectly()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Sku = "P1", Stock = 10, MinimumStock = 10, IsActive = true }, // LOW (equal)
            new() { Sku = "P2", Stock = 2, MinimumStock = 10, IsActive = true },  // LOW (less)
            new() { Sku = "P3", Stock = 0, MinimumStock = 5, IsActive = true },   // LOW (zero)
            new() { Sku = "P4", Stock = 20, MinimumStock = 10, IsActive = true }, // OK (greater)
            new() { Sku = "P5", Stock = 1, MinimumStock = 10, IsActive = false }  // INACTIVE (ignored)
        }.AsQueryable();

        // Act
        var lowStock = products.WhereLowStock().ToList();

        // Assert
        Assert.Equal(3, lowStock.Count);
        Assert.Contains(lowStock, p => p.Sku == "P1");
        Assert.Contains(lowStock, p => p.Sku == "P2");
        Assert.Contains(lowStock, p => p.Sku == "P3");
        Assert.DoesNotContain(lowStock, p => p.Sku == "P4");
        Assert.DoesNotContain(lowStock, p => p.Sku == "P5");
    }
}
