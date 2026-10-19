using System.Security.Claims;
using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RentalsController : ControllerBase
{
    private readonly IRentalService _rentalService;
    private readonly ICustomerRepository _customerRepo;

    public RentalsController(IRentalService rentalService, ICustomerRepository customerRepo)
    {
        _rentalService = rentalService;
        _customerRepo = customerRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RentalDto>>> GetAll([FromQuery] Guid? customerId, CancellationToken ct)
    {
        // Mitigación IDOR: Clientes solo ven sus propios contratos de alquiler
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null)
            {
                return Ok(Array.Empty<RentalDto>());
            }

            customerId = customer.Id;
        }

        var rentals = await _rentalService.GetAllRentalsAsync(customerId, ct);
        return Ok(rentals);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RentalDto>> GetById(Guid id, CancellationToken ct)
    {
        var rental = await _rentalService.GetRentalByIdAsync(id, ct);

        // Mitigación IDOR: Retornar 404 si el alquiler no pertenece al cliente (evita enumeración de IDs)
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null || rental.CustomerId != customer.Id)
            {
                return NotFound(new { message = $"Alquiler con ID {id} no encontrado." });
            }
        }

        return Ok(rental);
    }

    [HttpPost("quote")]
    public async Task<ActionResult<RentalQuoteDto>> CalculateQuote(
        [FromQuery] Guid vehicleId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var quote = await _rentalService.CalculateRentalQuoteAsync(vehicleId, startDate, endDate, cancellationToken: ct);
        return Ok(quote);
    }

    [HttpPost]
    public async Task<ActionResult<RentalDto>> Create([FromBody] CreateRentalDto dto, CancellationToken ct)
    {
        // Mitigación IDOR: El cliente no puede alquilar a nombre de un tercero
        if (!User.IsInRole("Administrador"))
        {
            var customer = await GetCurrentCustomerAsync(ct);
            if (customer is null)
            {
                return BadRequest(new { message = "Debe completar su perfil de cliente en /api/Customers antes de solicitar un alquiler." });
            }

            dto.CustomerId = customer.Id;
        }
        else if (dto.CustomerId == Guid.Empty)
        {
            return BadRequest(new { message = "El identificador del cliente es obligatorio para contratos creados por administradores." });
        }

        var rental = await _rentalService.CreateRentalAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = rental.Id }, rental);
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
    public async Task<ActionResult<RentalDto>> Cancel(Guid id, CancellationToken ct)
    {
        var rental = await _rentalService.CancelRentalAsync(id, ct);
        return Ok(rental);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _rentalService.CancelRentalAsync(id, ct);
        return NoContent();
    }
}
