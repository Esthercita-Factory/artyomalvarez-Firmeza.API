using System.Security.Claims;
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

    [HttpGet("my-profile")]
    [Authorize(Roles = "Cliente")]
    public async Task<ActionResult<CustomerDto>> GetMyProfile(CancellationToken ct = default)
    {
        var customer = await GetCurrentCustomerAsync(ct);
        if (customer is null)
            return NotFound(new { message = "No se encontró un perfil de cliente asociado a su cuenta de usuario." });

        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(id, ct);
        if (customer is null) return NotFound(new { message = $"Cliente con ID {id} no encontrado." });

        // Mitigación IDOR: si no es admin, solo puede consultar su propio registro
        if (!User.IsInRole("Administrador") && !IsOwner(customer))
        {
            return Forbid();
        }

        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    [HttpGet("by-document/{documentNumber}")]
    public async Task<ActionResult<CustomerDto>> GetByDocument(string documentNumber, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByDocumentAsync(documentNumber, ct);
        if (customer is null) return NotFound(new { message = $"Cliente con documento '{documentNumber}' no encontrado." });

        // Mitigación IDOR: un cliente no puede consultar documentos de terceros
        if (!User.IsInRole("Administrador") && !IsOwner(customer))
        {
            return Forbid();
        }

        return Ok(_mapper.Map<CustomerDto>(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto, CancellationToken ct = default)
    {
        // Si es rol Cliente, vincular al UserId autenticado y validar correo
        if (!User.IsInRole("Administrador"))
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(userEmail) && !dto.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "El correo del perfil de cliente debe coincidir con su correo de cuenta autenticada." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var existingProfile = await _customerRepo.GetByUserIdAsync(userId, ct);
                if (existingProfile != null)
                {
                    return BadRequest(new { message = "Ya existe un perfil de cliente asociado a su cuenta de usuario." });
                }
            }
        }

        var existingDoc = await _customerRepo.GetByDocumentAsync(dto.DocumentNumber, ct);
        if (existingDoc is not null)
            return BadRequest(new { message = $"Ya existe un cliente con el documento '{dto.DocumentNumber}'." });

        var existingEmail = await _customerRepo.GetByEmailAsync(dto.Email, ct);
        if (existingEmail is not null)
            return BadRequest(new { message = $"Ya existe un cliente con el correo '{dto.Email}'." });

        var customer = _mapper.Map<Customer>(dto);
        if (!User.IsInRole("Administrador"))
        {
            customer.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

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

        // Mitigación IDOR: clientes solo modifican su propio perfil
        if (!User.IsInRole("Administrador") && !IsOwner(customer))
        {
            return Forbid();
        }

        _mapper.Map(dto, customer);
        customer.UpdatedAtUtc = DateTime.UtcNow;

        _customerRepo.Update(customer);
        await _uow.SaveChangesAsync(ct);

        return Ok(_mapper.Map<CustomerDto>(customer));
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
            return await _customerRepo.GetByEmailAsync(userEmail, ct);
        }

        return null;
    }

    private bool IsOwner(Customer customer)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(customer.UserId))
        {
            return customer.UserId == userId;
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        return !string.IsNullOrEmpty(userEmail) && customer.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase);
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
