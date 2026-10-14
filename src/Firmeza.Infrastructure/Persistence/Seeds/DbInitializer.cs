using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Firmeza.Infrastructure.Persistence.Seeds;

public static class DbInitializer
{
    public const string RoleAdmin = "Administrador";
    public const string RoleCliente = "Cliente";

    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // 1. Crear roles si no existen
        string[] roles = [RoleAdmin, RoleCliente];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol creado: {Role}", role);
            }
        }

        // 2. Crear administrador solo si la contraseña está configurada de forma segura por variables de entorno o user-secrets
        var adminPassword = configuration["AdminUser:Password"];
        var adminEmail = configuration["AdminUser:Email"] ?? "admin@firmeza.com";

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("No se configuró 'AdminUser:Password' en variables de entorno o almacén seguro. Se omite la creación del usuario administrador por defecto.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, RoleAdmin);
                logger.LogInformation("Usuario administrador creado con éxito: {Email}", adminEmail);
            }
            else
            {
                logger.LogWarning("No se pudo crear el usuario administrador: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
