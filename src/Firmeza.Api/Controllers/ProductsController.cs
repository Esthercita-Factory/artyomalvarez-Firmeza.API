using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(CancellationToken ct)
        => Ok(await _db.Products.AsNoTracking().ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product input, CancellationToken ct)
    {
        var product = new Product
        {
            Sku = input.Sku,
            Name = input.Name,
            Description = input.Description,
            Category = input.Category,
            UnitOfMeasure = string.IsNullOrWhiteSpace(input.UnitOfMeasure) ? "unidad" : input.UnitOfMeasure,
            UnitPrice = input.UnitPrice,
            Stock = input.Stock,
            MinimumStock = input.MinimumStock,
            IsActive = input.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Product input, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();

        product.Sku = input.Sku;
        product.Name = input.Name;
        product.Description = input.Description;
        product.Category = input.Category;
        product.UnitOfMeasure = input.UnitOfMeasure;
        product.UnitPrice = input.UnitPrice;
        product.Stock = input.Stock;
        product.MinimumStock = input.MinimumStock;
        product.IsActive = input.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}