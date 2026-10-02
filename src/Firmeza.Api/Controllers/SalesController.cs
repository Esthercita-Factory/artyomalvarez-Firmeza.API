using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SalesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sale>>> GetAll(CancellationToken ct)
        => Ok(await _db.Sales.AsNoTracking().ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Sale>> GetById(int id, CancellationToken ct)
    {
        var sale = await _db.Sales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return sale is null ? NotFound() : Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<Sale>> Create([FromBody] Sale input, CancellationToken ct)
    {
        var sale = new Sale
        {
            CustomerId = input.CustomerId,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Pendiente" : input.Status,
            ExternalReference = input.ExternalReference,
            Subtotal = input.Subtotal,
            TaxRate = input.TaxRate,
            TaxAmount = input.TaxAmount,
            Total = input.Total,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Sale input, CancellationToken ct)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sale is null) return NotFound();

        sale.CustomerId = input.CustomerId;
        sale.Status = input.Status;
        sale.ExternalReference = input.ExternalReference;
        sale.Subtotal = input.Subtotal;
        sale.TaxRate = input.TaxRate;
        sale.TaxAmount = input.TaxAmount;
        sale.Total = input.Total;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sale is null) return NotFound();

        _db.Sales.Remove(sale);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
