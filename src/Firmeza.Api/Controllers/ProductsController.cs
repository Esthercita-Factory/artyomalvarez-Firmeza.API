using AutoMapper;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
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
    private readonly IPdfService _pdfService;

    public ProductsController(IProductRepository productRepo, IUnitOfWork uow, IMapper mapper, IPdfService pdfService)
    {
        _productRepo = productRepo;
        _uow = uow;
        _mapper = mapper;
        _pdfService = pdfService;
    }

    [HttpGet("export-pdf")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf([FromQuery] bool onlyActive = false, CancellationToken ct = default)
    {
        var products = await _productRepo.GetAllAsync(onlyActive, null, null, ct);
        var dtos = _mapper.Map<IReadOnlyList<ProductDto>>(products);
        var bytes = await _pdfService.GenerateProductsCatalogPdfAsync(dtos, ct);
        return File(bytes, "application/pdf", $"Catalogo-Materiales-Firmeza-{DateTime.UtcNow:yyyyMMddHHmm}.pdf");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool onlyActive = true,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var (products, totalCount) = await _productRepo.GetPagedAsync(
            onlyActive, search, category, status, lowStock, page, pageSize, ct);

        var dtos = _mapper.Map<IReadOnlyList<ProductDto>>(products);
        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            return Ok(new
            {
                items = dtos,
                totalCount,
                page = page.Value,
                pageSize = pageSize.Value,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize.Value)
            });
        }

        return Ok(dtos);
    }

    [HttpGet("low-stock")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetLowStock(CancellationToken ct = default)
    {
        var products = await _productRepo.GetLowStockAsync(ct);
        return Ok(_mapper.Map<IReadOnlyList<ProductDto>>(products));
    }

    [HttpGet("low-stock/count")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetLowStockCount(CancellationToken ct = default)
    {
        var count = await _productRepo.GetLowStockCountAsync(ct);
        return Ok(new { count });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
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

        if (string.IsNullOrWhiteSpace(dto.Category))
            dto.Category = "General";

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

        if (string.IsNullOrWhiteSpace(dto.Category))
            dto.Category = product.Category ?? "General";

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