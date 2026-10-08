using AutoMapper;
using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomersController(ICustomerRepository customerRepo, IUnitOfWork uow, IMapper mapper)
    {
        _customerRepo = customerRepo;
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct = default)
    {
        var customers = await _customerRepo.GetAllAsync(ct);
        return Ok(_mapper.Map<IReadOnlyList<CustomerDto>>(customers));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        if (customer is null) return NotFound(new { message = $"Cliente con ID {id} no encontrado." });
        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    [HttpGet("by-document/{documentNumber}")]
    public async Task<ActionResult<CustomerDto>> GetByDocument(string documentNumber, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByDocumentAsync(documentNumber, ct);
        if (customer is null) return NotFound(new { message = $"Cliente con documento '{documentNumber}' no encontrado." });
        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto, CancellationToken ct = default)
    {
        var existingDoc = await _customerRepo.GetByDocumentAsync(dto.DocumentNumber, ct);
        if (existingDoc is not null)
            return BadRequest(new { message = $"Ya existe un cliente con el documento '{dto.DocumentNumber}'." });

        var existingEmail = await _customerRepo.GetByEmailAsync(dto.Email, ct);
        if (existingEmail is not null)
            return BadRequest(new { message = $"Ya existe un cliente con el correo '{dto.Email}'." });

        var customer = _mapper.Map<Customer>(dto);
        await _customerRepo.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);

        var resultDto = _mapper.Map<CustomerDto>(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, resultDto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        if (customer is null) return NotFound(new { message = $"Cliente con ID {id} no encontrado." });

        _mapper.Map(dto, customer);
        customer.UpdatedAtUtc = DateTime.UtcNow;

        _customerRepo.Update(customer);
        await _uow.SaveChangesAsync(ct);

        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        if (customer is null) return NotFound(new { message = $"Cliente con ID {id} no encontrado." });

        _customerRepo.Delete(customer);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }
}
