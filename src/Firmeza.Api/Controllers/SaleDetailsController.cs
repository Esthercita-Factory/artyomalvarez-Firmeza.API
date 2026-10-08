using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SaleDetailsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SaleDetailsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleDetail>>> GetAll(CancellationToken ct)
        => Ok(await _db.SaleDetails.AsNoTracking().ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDetail>> GetById(Guid id, CancellationToken ct)
    {
        var saleDetail = await _db.SaleDetails.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        return saleDetail is null ? NotFound() : Ok(saleDetail);
    }

    [HttpPost]
    public async Task<ActionResult<SaleDetail>> Create([FromBody] SaleDetail input, CancellationToken ct)
    {
        var saleDetail = new SaleDetail
        {
            SaleId = input.SaleId,
            ProductId = input.ProductId,
            Quantity = input.Quantity,
            UnitPrice = input.UnitPrice,
            LineTotal = input.LineTotal
        };

        _db.SaleDetails.Add(saleDetail);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = saleDetail.Id }, saleDetail);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaleDetail input, CancellationToken ct)
    {
        var saleDetail = await _db.SaleDetails.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (saleDetail is null) return NotFound();

        saleDetail.SaleId = input.SaleId;
        saleDetail.ProductId = input.ProductId;
        saleDetail.Quantity = input.Quantity;
        saleDetail.UnitPrice = input.UnitPrice;
        saleDetail.LineTotal = input.LineTotal;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var saleDetail = await _db.SaleDetails.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (saleDetail is null) return NotFound();

        _db.SaleDetails.Remove(saleDetail);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
