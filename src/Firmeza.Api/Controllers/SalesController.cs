using System.Security.Claims;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ICustomerRepository _customerRepo;

    public SalesController(ISaleService saleService, ICustomerRepository customerRepo)
    {
        _saleService = saleService;
        _customerRepo = customerRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleDto>>> GetAll([FromQuery] Guid? customerId, CancellationToken ct)
    {
        // Mitigación IDOR: Clientes solo ven sus propias ventas
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null)
            {
                return Ok(Array.Empty<SaleDto>());
            }

            customerId = customer.Id;
        }

        var sales = await _saleService.GetAllSalesAsync(customerId, ct);
        return Ok(sales);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken ct)
    {
        var sale = await _saleService.GetSaleByIdAsync(id, ct);

        // Mitigación IDOR: Retornar 404 si la venta no pertenece al cliente (evita enumeración de IDs)
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null || sale.CustomerId != customer.Id)
            {
                return NotFound(new { message = $"Venta con ID {id} no encontrada." });
            }
        }

        return Ok(sale);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken ct)
    {
        var sale = await _saleService.GetSaleByIdAsync(id, ct);

        // Mitigación IDOR: Solo el dueño de la venta o el admin puede descargar el recibo
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null || sale.CustomerId != customer.Id)
            {
                return NotFound(new { message = $"Venta con ID {id} no encontrada." });
            }
        }

        var pdfBytes = await _saleService.GetSaleReceiptBytesAsync(id, ct);
        return File(pdfBytes, "application/pdf", $"Recibo-{id}.pdf");
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
        // Mitigación IDOR: Un cliente no puede forzar la compra a nombre de otro CustomerId
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null)
            {
                return BadRequest(new { message = "Debe completar su perfil de cliente en /api/Customers antes de realizar compras." });
            }

            dto.CustomerId = customer.Id;
        }

        var sale = await _saleService.CreateSaleAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    private async Task<Customer?> GetCurrentCustomerAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var customer = await _customerRepo.GetByUserIdAsync(userId, ct);
            if (customer != null) return customer;
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(userEmail))
        {
            var customer = await _customerRepo.GetByEmailAsync(userEmail, ct);
            if (customer != null) return customer;
        }

        // Auto-provisioning resiliente para cuentas de cliente autenticadas sin perfil previo
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userEmail))
        {
            return await _customerRepo.GetOrCreateForUserAsync(userId, userEmail, ct);
        }

        return null;
    }

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<SaleDto>> Cancel(Guid id, CancellationToken ct)
    {
        var sale = await _saleService.CancelSaleAsync(id, ct);
        return Ok(sale);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _saleService.CancelSaleAsync(id, ct);
        return NoContent();
    }
}
