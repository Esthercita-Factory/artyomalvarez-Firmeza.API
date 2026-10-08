using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleDto>>> GetAll([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var sales = await _saleService.GetAllSalesAsync(customerId, ct);
        return Ok(sales);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken ct)
    {
        var sale = await _saleService.GetSaleByIdAsync(id, ct);
        return Ok(sale);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<CartCalculationDto>> CalculateCart([FromBody] List<CartItemDto> items, [FromQuery] decimal taxRate = 0.19m, CancellationToken ct = default)
    {
        var calculation = await _saleService.CalculateCartAsync(items, taxRate, ct);
        return Ok(calculation);
    }

    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create([FromBody] CreateSaleDto dto, CancellationToken ct)
    {
        var sale = await _saleService.CreateSaleAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }
}
