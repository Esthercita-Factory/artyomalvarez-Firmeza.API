using AutoMapper;
using Firmeza.Application.DTOs.Vehicles;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public VehiclesController(IVehicleRepository vehicleRepo, IUnitOfWork uow, IMapper mapper)
    {
        _vehicleRepo = vehicleRepo;
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> GetAll(
        [FromQuery] VehicleStatus? status,
        [FromQuery] VehicleType? type,
        CancellationToken ct)
    {
        var vehicles = await _vehicleRepo.GetAllAsync(status, type, ct);
        return Ok(_mapper.Map<IReadOnlyList<VehicleDto>>(vehicles));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> GetById(Guid id, CancellationToken ct)
    {
        var vehicle = await _vehicleRepo.GetByIdAsync(id, ct);
        if (vehicle is null) return NotFound();
        return Ok(_mapper.Map<VehicleDto>(vehicle));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] CreateVehicleDto dto, CancellationToken ct)
    {
        var existing = await _vehicleRepo.GetByPlateAsync(dto.Plate, ct);
        if (existing is not null)
            return BadRequest(new { message = $"Ya existe un vehículo registrado con la placa {dto.Plate}." });

        var vehicle = _mapper.Map<Vehicle>(dto);
        await _vehicleRepo.AddAsync(vehicle, ct);
        await _uow.SaveChangesAsync(ct);

        var createdDto = _mapper.Map<VehicleDto>(vehicle);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, createdDto);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto dto, CancellationToken ct)
    {
        var vehicle = await _vehicleRepo.GetByIdAsync(id, ct);
        if (vehicle is null) return NotFound();

        _mapper.Map(dto, vehicle);
        _vehicleRepo.Update(vehicle);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var vehicle = await _vehicleRepo.GetByIdAsync(id, ct);
        if (vehicle is null) return NotFound();

        _vehicleRepo.Delete(vehicle);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }
}
