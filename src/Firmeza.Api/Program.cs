using System.Text;
using Firmeza.Api.Middlewares;
using Firmeza.Application;
using Firmeza.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// CORS configurable por entorno (con fallback a localhost:4200)
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "FirmezaDevelopmentSecretKeyForJwtTokenGeneration2026!";
    }
    else
    {
        throw new InvalidOperationException("La clave 'Jwt:Key' es obligatoria y debe tener al menos 32 caracteres en entornos no-Development.");
    }
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FirmezaApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FirmezaClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Registrar controladores conectando FluentValidationFilter al pipeline
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
});

var app = builder.Build();

// Capa de Seguridad y Manejo Global de Excepciones
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Firmeza API Documentation")
               .AddPreferredSecuritySchemes("Bearer")
               .AddHttpAuthentication("Bearer", _ => { });
    });
}

app.UseHttpsRedirection();

// Habilitar archivos estáticos para la descarga de recibos en wwwroot
var receiptsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "recibos");
if (!Directory.Exists(receiptsPath))
{
    Directory.CreateDirectory(receiptsPath);
}
app.UseStaticFiles();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

// Inicializar roles y admin por defecto solo en desarrollo o si está explícitamente activado por configuración
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SeedData:Enabled", false))
{
    await Firmeza.Infrastructure.Persistence.Seeds.DbInitializer.SeedRolesAndAdminAsync(app.Services, app.Configuration);
}

app.MapControllers();
app.Run();
