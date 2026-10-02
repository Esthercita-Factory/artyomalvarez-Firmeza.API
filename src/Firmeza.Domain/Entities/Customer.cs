namespace Firmeza.Domain.Entities;

public class Customer
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public string DocumentType { get; set; } = "Cédula";

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public int? Age { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}