namespace Firmeza.Application.DTOs.Excel;

public class ExcelProductRowDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "unidad";
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public string? Description { get; set; }
}

public class ExcelCustomerRowDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "CC";
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int? Age { get; set; }
}

public class ExcelImportResultDto
{
    public int TotalRows { get; set; }
    public int ProcessedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
