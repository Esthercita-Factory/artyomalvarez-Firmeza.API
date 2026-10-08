namespace Firmeza.Application.DTOs.Sales;

public class SaleDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public List<SaleDetailDto> Details { get; set; } = new();
}

public class SaleDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class CartItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateSaleDto
{
    public Guid CustomerId { get; set; }
    public string? ExternalReference { get; set; }
    public decimal TaxRate { get; set; } = 0.19m; // 19% IVA por defecto
    public List<CartItemDto> Items { get; set; } = new();
}

public class CartCalculationDto
{
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public List<SaleDetailDto> Items { get; set; } = new();
}
