using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RentalsController : ControllerBase
{
    private readonly IRentalService _rentalService;

    public RentalsController(IRentalService rentalService)
    {
        _rentalService = rentalService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RentalDto>>> GetAll([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var rentals = await _rentalService.GetAllRentalsAsync(customerId, ct);
        return Ok(rentals);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RentalDto>> GetById(Guid id, CancellationToken ct)
    {
        var rental = await _rentalService.GetRentalByIdAsync(id, ct);
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
        var rental = await _rentalService.CreateRentalAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = rental.Id }, rental);
    }
}
