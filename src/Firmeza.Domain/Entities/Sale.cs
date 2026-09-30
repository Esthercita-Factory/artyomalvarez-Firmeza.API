namespace Firmeza.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
}
