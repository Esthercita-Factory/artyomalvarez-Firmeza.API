using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CustomersController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll(CancellationToken ct)
        => Ok(await _db.Customers.AsNoTracking().ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Customer>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] Customer input, CancellationToken ct)
    {
        var customer = new Customer
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            DocumentNumber = input.DocumentNumber,
            DocumentType = string.IsNullOrWhiteSpace(input.DocumentType) ? "Cédula" : input.DocumentType,
            Email = input.Email,
            Phone = input.Phone,
            Address = input.Address,
            Age = input.Age,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Customer input, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();

        customer.FirstName = input.FirstName;
        customer.LastName = input.LastName;
        customer.DocumentNumber = input.DocumentNumber;
        customer.DocumentType = input.DocumentType;
        customer.Email = input.Email;
        customer.Phone = input.Phone;
        customer.Address = input.Address;
        customer.Age = input.Age;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
