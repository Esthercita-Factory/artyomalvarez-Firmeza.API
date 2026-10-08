using Firmeza.Domain.Common;

namespace Firmeza.Domain.Entities;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public string DocumentType { get; set; } = "CC";

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public int? Age { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
}