using AutoMapper;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductsController(IProductRepository productRepo, IUnitOfWork uow, IMapper mapper)
    {
        _productRepo = productRepo;
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var products = await _productRepo.GetAllAsync(onlyActive, ct);
        return Ok(_mapper.Map<IReadOnlyList<ProductDto>>(products));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetLowStock(CancellationToken ct = default)
    {
        var products = await _productRepo.GetLowStockAsync(ct);
        return Ok(_mapper.Map<IReadOnlyList<ProductDto>>(products));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct);
        if (product is null) return NotFound(new { message = $"Producto con ID {id} no encontrado." });
        return Ok(_mapper.Map<ProductDto>(product));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto, CancellationToken ct = default)
    {
        var existing = await _productRepo.GetBySkuAsync(dto.Sku, ct);
        if (existing is not null)
            return BadRequest(new { message = $"Ya existe un producto con el SKU '{dto.Sku}'." });

        var product = _mapper.Map<Product>(dto);
        await _productRepo.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var resultDto = _mapper.Map<ProductDto>(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, resultDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct);
        if (product is null) return NotFound(new { message = $"Producto con ID {id} no encontrado." });

        _mapper.Map(dto, product);
        product.UpdatedAtUtc = DateTime.UtcNow;

        _productRepo.Update(product);
        await _uow.SaveChangesAsync(ct);

        return Ok(_mapper.Map<ProductDto>(product));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct);
        if (product is null) return NotFound(new { message = $"Producto con ID {id} no encontrado." });

        _productRepo.Delete(product);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }
}