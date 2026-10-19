using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICustomerRepository _customerRepo;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ICustomerRepository customerRepo,
        IConfiguration config,
        IWebHostEnvironment env,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _customerRepo = customerRepo;
        _config = config;
        _env = env;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (!new EmailAddressAttribute().IsValid(request.Email))
            return BadRequest(new { message = "El formato del correo electrónico no es válido." });

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Conflict(new { message = "User with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        await _userManager.AddToRoleAsync(user, "Cliente");
        await _customerRepo.GetOrCreateForUserAsync(user.Id, user.Email!);

        // Enviar notificación de bienvenida por correo electrónico (S3-T8)
        try
        {
            var subject = "¡Bienvenido/a a Firmeza!";
            var body = $@"
                <h2>¡Bienvenido/a a Firmeza!</h2>
                <p>Tu cuenta ha sido creada exitosamente con el correo: <strong>{user.Email}</strong>.</p>
                <p>Ya puedes consultar nuestro catálogo de materiales de construcción y cotizar alquiler de flota pesada.</p>
                <p>Atentamente,<br><strong>Equipo Firmeza</strong></p>";

            await _emailService.SendEmailAsync(user.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo despachar el correo de bienvenida a {Email}", user.Email);
        }

        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var userRoles = await _userManager.GetRolesAsync(user);

        var configuredKey = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey.Length < 32)
        {
            throw new InvalidOperationException("La clave JWT no está configurada o es demasiado corta (mínimo 32 caracteres).");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuredKey);
        var issuer = _config["Jwt:Issuer"] ?? "FirmezaApi";
        var audience = _config["Jwt:Audience"] ?? "FirmezaClient";
        var expiresAt = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new AuthResponse(tokenString, expiresAt, user.Id, user.Email, userRoles));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoResponse>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserInfoResponse(user.Id, user.Email, user.UserName, roles));
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, DateTime ExpiresAt, string UserId, string? Email, IList<string> Roles);
public record UserInfoResponse(string Id, string? Email, string? UserName, IList<string> Roles);
