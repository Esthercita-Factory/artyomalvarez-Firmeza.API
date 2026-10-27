using Bogus;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Firmeza.Infrastructure.Persistence.Seeds;

public static class DataSeeder
{
    public const string DefaultUserPassword = "Cliente123*Password";

    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        logger.LogInformation("🌱 Iniciando verificación y siembra de datos semilla (DataSeeder)...");

        // Fijar semilla Bogus para reproducibilidad exacta
        Randomizer.Seed = new Random(20261020);

        // 0. Asegurar roles
        await EnsureRolesAsync(roleManager, logger);

        // 1. Limpieza de datos espurios de pruebas manuales previas (ej. "Chuchillo", "TANQUE T1000")
        await CleanupJunkDataAsync(context, logger);

        // 2. Administrador y Cliente de Prueba
        await SeedKeyAccountsAsync(userManager, context, configuration, logger);

        // 3. 20 Clientes con usuarios Identity
        await SeedCustomersAsync(userManager, context, logger);

        // 4. 50 Productos clasificados en 8 categorías reales
        await SeedProductsAsync(context, logger);

        // 5. 15 Vehículos y Maquinaria Pesada
        await SeedVehiclesAsync(context, logger);

        // 6. 40 Ventas con cálculo de stock e IVA 19%
        await SeedSalesAsync(context, logger);

        // 7. 15 Alquileres coherentes con la flota
        await SeedRentalsAsync(context, logger);

        // 8. Resumen estadístico
        await PrintDatabaseSummaryAsync(context, userManager, logger);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = [DbInitializer.RoleAdmin, DbInitializer.RoleCliente];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("  [Rol] Creado: {Role}", role);
            }
        }
    }

    private static async Task CleanupJunkDataAsync(ApplicationDbContext context, ILogger logger)
    {
        var junkProducts = await context.Products
            .Where(p => p.Name.Contains("Chuchillo") || p.Sku.StartsWith("TEST-"))
            .ToListAsync();

        if (junkProducts.Count != 0)
        {
            context.Products.RemoveRange(junkProducts);
            await context.SaveChangesAsync();
            logger.LogInformation("  [Limpieza] Eliminados {Count} productos temporales/inválidos de prueba.", junkProducts.Count);
        }

        var junkVehicles = await context.Vehicles
            .Where(v => v.Plate.Contains("WOLF") || v.Model.Contains("T1000"))
            .ToListAsync();

        if (junkVehicles.Count != 0)
        {
            context.Vehicles.RemoveRange(junkVehicles);
            await context.SaveChangesAsync();
            logger.LogInformation("  [Limpieza] Eliminados {Count} vehículos de prueba con datos no realistas.", junkVehicles.Count);
        }
    }

    private static async Task SeedKeyAccountsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        // A. Administrador principal
        var adminEmail = configuration["AdminUser:Email"] ?? "admin@firmeza.com";
        var adminPassword = configuration["AdminUser:Password"] ?? "Admin123*Password";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(adminUser, adminPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, DbInitializer.RoleAdmin);
                logger.LogInformation("  [Cuenta Clave] Administrador creado: {Email}", adminEmail);
            }
        }

        var adminCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Email == adminEmail);
        if (adminCustomer == null)
        {
            adminCustomer = new Customer
            {
                FirstName = "Administrador",
                LastName = "Firmeza",
                DocumentType = "NIT",
                DocumentNumber = "901548231-5",
                Email = adminEmail,
                Phone = "3001234567",
                Address = "Calle 100 # 15-20, Oficina 402, Bogotá",
                UserId = adminUser?.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            context.Customers.Add(adminCustomer);
            await context.SaveChangesAsync();
        }

        // B. Cliente de prueba B2B
        const string testClientEmail = "testcliente@firmeza.com";
        var testClientUser = await userManager.FindByEmailAsync(testClientEmail);
        if (testClientUser == null)
        {
            testClientUser = new ApplicationUser
            {
                UserName = testClientEmail,
                Email = testClientEmail,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(testClientUser, DefaultUserPassword);
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(testClientUser, DbInitializer.RoleCliente);
                logger.LogInformation("  [Cuenta Clave] Cliente de prueba creado: {Email}", testClientEmail);
            }
        }

        var testCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Email == testClientEmail);
        if (testCustomer == null)
        {
            testCustomer = new Customer
            {
                FirstName = "Constructora",
                LastName = "Andina B2B",
                DocumentType = "NIT",
                DocumentNumber = "900784512-3",
                Email = testClientEmail,
                Phone = "3154567890",
                Address = "Carrera 43A # 16A-28, Medellín",
                UserId = testClientUser?.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            context.Customers.Add(testCustomer);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedCustomersAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger logger)
    {
        var existingCount = await context.Customers.CountAsync();
        if (existingCount >= 20)
        {
            logger.LogInformation("  [Clientes] Ya existen {Count} clientes en la base de datos. Omitiendo generación.", existingCount);
            return;
        }

        var faker = new Faker("es");
        var customersNeeded = 22 - existingCount; // total objetivo: ~22 clientes
        var created = 0;

        string[] ciudades = ["Bogotá D.C.", "Medellín", "Cali", "Barranquilla", "Bucaramanga", "Cartagena", "Pereira", "Manizales"];
        string[] tiposDoc = ["CC", "NIT"];

        for (int i = 1; i <= customersNeeded; i++)
        {
            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();
            var docType = i % 3 == 0 ? "NIT" : "CC";
            var docNumber = docType == "NIT" 
                ? $"901{faker.Random.Number(100000, 999999)}-{faker.Random.Number(1, 9)}"
                : $"10{faker.Random.Number(10000000, 99999999)}";

            var email = $"cliente.{faker.Random.AlphaNumeric(5).ToLowerInvariant()}@constructora.com.co";
            var phone = $"3{faker.Random.Number(10, 25)}{faker.Random.Number(1000000, 9999999)}";
            var ciudad = faker.PickRandom(ciudades);
            var address = $"{faker.Address.StreetAddress()}, {ciudad}";

            // Verificar si el documento o correo ya existen
            if (await context.Customers.AnyAsync(c => c.DocumentNumber == docNumber || c.Email == email))
                continue;

            // 1. Crear Identity User
            var appUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = phone
            };

            var userResult = await userManager.CreateAsync(appUser, DefaultUserPassword);
            if (userResult.Succeeded)
            {
                await userManager.AddToRoleAsync(appUser, DbInitializer.RoleCliente);

                // 2. Crear Customer vinculado
                var customer = new Customer
                {
                    FirstName = firstName,
                    LastName = lastName,
                    DocumentType = docType,
                    DocumentNumber = docNumber,
                    Email = email,
                    Phone = phone,
                    Address = address,
                    UserId = appUser.Id,
                    Age = faker.Random.Number(25, 60),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-faker.Random.Number(10, 180))
                };

                context.Customers.Add(customer);
                created++;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("  [Clientes] Creados {Count} clientes B2B con usuarios Identity vinculados.", created);
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingCount = await context.Products.CountAsync();
        if (existingCount >= 50)
        {
            logger.LogInformation("  [Productos] Ya existen {Count} productos en catálogo. Omitiendo generación.", existingCount);
            return;
        }

        // Catálogo realista de 50 productos en 8 categorías formales con marcas líderes en Colombia
        var productCatalog = new List<Product>
        {
            // 1. Cementos y morteros (7 productos)
            new() { Sku = "CEM-001", Name = "Cemento Gris Tipo 1 (50kg)", Brand = "Argos", Category = "Cementos y morteros", UnitOfMeasure = "saco 50kg", UnitPrice = 34500m, Stock = 450, MinimumStock = 50, Description = "Cemento para uso general en estructuras, losas y muros de concreto." },
            new() { Sku = "CEM-002", Name = "Cemento Blanco Extra Fino (40kg)", Brand = "Cemex", Category = "Cementos y morteros", UnitOfMeasure = "saco 40kg", UnitPrice = 48000m, Stock = 210, MinimumStock = 30, Description = "Cemento blanco para acabados arquitectónicos, pegas y texturas." },
            new() { Sku = "CEM-003", Name = "Mortero Seco Listo para Muros (50kg)", Brand = "Holcim", Category = "Cementos y morteros", UnitOfMeasure = "saco 50kg", UnitPrice = 28900m, Stock = 320, MinimumStock = 40, Description = "Mezcla predosificada lista para pega de mampostería y pañete." },
            new() { Sku = "CEM-004", Name = "Cemento Estructural Alta Resistencia (50kg)", Brand = "Argos", Category = "Cementos y morteros", UnitOfMeasure = "saco 50kg", UnitPrice = 38500m, Stock = 380, MinimumStock = 60, Description = "Cemento especializado para zapatas, vigas y columnas de alta carga." },
            new() { Sku = "CEM-005", Name = "Mortero Impermeabilizante SikaTop 107 (25kg)", Brand = "Sika", Category = "Cementos y morteros", UnitOfMeasure = "saco 25kg", UnitPrice = 72000m, Stock = 140, MinimumStock = 20, Description = "Recubrimiento impermeable para tanques de agua, cimientos y muros." },
            new() { Sku = "CEM-006", Name = "Mortero Nivelador de Pisos Ultra (25kg)", Brand = "Corona", Category = "Cementos y morteros", UnitOfMeasure = "saco 25kg", UnitPrice = 32500m, Stock = 190, MinimumStock = 25, Description = "Mortero fluido para nivelación rápida antes de enchapes." },
            new() { Sku = "CEM-007", Name = "Cemento Gris Uso General (42.5kg)", Brand = "Holcim", Category = "Cementos y morteros", UnitOfMeasure = "saco 42.5kg", UnitPrice = 31900m, Stock = 280, MinimumStock = 40, Description = "Cemento hidráulico para obras residenciales y comerciales." },

            // 2. Agregados (arena, grava) (7 productos)
            new() { Sku = "AGR-001", Name = "Arena Lavada de Río Clasificada (m³)", Brand = "Agregados del Norte", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 65000m, Stock = 180, MinimumStock = 30, Description = "Arena fina lavada libre de lodos para concretos y pañetes finos." },
            new() { Sku = "AGR-002", Name = "Arena de Peña Zarandeada (m³)", Brand = "Canteras de la Sabana", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 52000m, Stock = 150, MinimumStock = 25, Description = "Arena de grano grueso recomendada para rellenos y morteros de pega." },
            new() { Sku = "AGR-003", Name = "Grava Común 3/4 Pulgada (m³)", Brand = "Agregados Nacionales", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 78000m, Stock = 130, MinimumStock = 20, Description = "Piedra triturada limpia de 3/4 para elaboración de concreto estructural." },
            new() { Sku = "AGR-004", Name = "Triturado Fino 1/2 Pulgada (m³)", Brand = "Canteras de la Sabana", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 82000m, Stock = 110, MinimumStock = 20, Description = "Triturado clasificado para losas delgadas y pisos industriales." },
            new() { Sku = "AGR-005", Name = "Base Granular Tipo B-200 (m³)", Brand = "Canteras del Valle", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 48000m, Stock = 200, MinimumStock = 40, Description = "Material certificado para conformación de vías y parqueaderos." },
            new() { Sku = "AGR-006", Name = "Sub-base Granular Clasificada (m³)", Brand = "Canteras del Valle", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 44000m, Stock = 220, MinimumStock = 40, Description = "Capa de soporte para pavimentos y plataformas de cimentación." },
            new() { Sku = "AGR-007", Name = "Piedra Rajón Selección Manual (m³)", Brand = "Agregados Nacionales", Category = "Agregados (arena, grava)", UnitOfMeasure = "m³", UnitPrice = 58000m, Stock = 90, MinimumStock = 15, Description = "Piedra de tamaño grande para gaviones, muros de contención y filtros." },

            // 3. Ladrillos y bloques (7 productos)
            new() { Sku = "LAD-001", Name = "Ladrillo Estructural Tolete Arcilla", Brand = "Ladrillera Santafé", Category = "Ladrillos y bloques", UnitOfMeasure = "unidad", UnitPrice = 1450m, Stock = 8500, MinimumStock = 1000, Description = "Ladrillo macizo prensado de alta resistencia a compresión." },
            new() { Sku = "LAD-002", Name = "Bloque de Arcilla No. 5 Hueco", Brand = "Ladrillera Santafé", Category = "Ladrillos y bloques", UnitOfMeasure = "unidad", UnitPrice = 1850m, Stock = 6200, MinimumStock = 800, Description = "Bloque liviano para muros divisorios y cerramientos interiores." },
            new() { Sku = "LAD-003", Name = "Ladrillo Limpio Fachada Rojo Granada", Brand = "Ladrillera Meléndez", Category = "Ladrillos y bloques", UnitOfMeasure = "unidad", UnitPrice = 2100m, Stock = 4500, MinimumStock = 600, Description = "Ladrillo a la vista para fachadas elegantes sin requerir pintura." },
            new() { Sku = "LAD-004", Name = "Bloque de Concreto Estructural 15x20x40", Brand = "Prefabricados Titán", Category = "Ladrillos y bloques", UnitOfMeasure = "unidad", UnitPrice = 3200m, Stock = 3100, MinimumStock = 500, Description = "Bloque de concreto pesado para muros de carga y bodegas." },
            new() { Sku = "LAD-005", Name = "Adoquín Peatonal Rectangular 6cm Gris", Brand = "Prefabricados Titán", Category = "Ladrillos y bloques", UnitOfMeasure = "m²", UnitPrice = 38500m, Stock = 400, MinimumStock = 50, Description = "Adoquines de alto tráfico para andenes, senderos y plazoletas." },
            new() { Sku = "LAD-006", Name = "Ladrillo Rejilla Liviano 10x20x40", Brand = "Ladrillera Meléndez", Category = "Ladrillos y bloques", UnitOfMeasure = "unidad", UnitPrice = 1650m, Stock = 5200, MinimumStock = 700, Description = "Elemento aligerante para muros divisorios no estructurales." },
            new() { Sku = "LAD-007", Name = "Adoquín Vehicular 8cm Tráfico Pesado", Brand = "Prefabricados Titán", Category = "Ladrillos y bloques", UnitOfMeasure = "m²", UnitPrice = 46000m, Stock = 350, MinimumStock = 40, Description = "Adoquín vehicular de alta compresión para vías y accesos logísticos." },

            // 4. Hierro y acero (7 productos)
            new() { Sku = "HIE-001", Name = "Varilla Corrugada 1/2 Pulgada Grado 60 (6m)", Brand = "Diaco", Category = "Hierro y acero", UnitOfMeasure = "varilla 6m", UnitPrice = 34500m, Stock = 650, MinimumStock = 100, Description = "Acero de refuerzo estándar para vigas, columnas y zapatas." },
            new() { Sku = "HIE-002", Name = "Varilla Corrugada 3/8 Pulgada Grado 60 (6m)", Brand = "Paz del Río", Category = "Hierro y acero", UnitOfMeasure = "varilla 6m", UnitPrice = 21200m, Stock = 800, MinimumStock = 120, Description = "Varilla para estribos y refuerzo secundario de concreto." },
            new() { Sku = "HIE-003", Name = "Malla Electrosoldada M-188 6x2.35m", Brand = "Diaco", Category = "Hierro y acero", UnitOfMeasure = "panel", UnitPrice = 128000m, Stock = 95, MinimumStock = 20, Description = "Refuerzo para losas de contrapiso, entrepisos y muros de contención." },
            new() { Sku = "HIE-004", Name = "Alambre Negro Recocido Calibre 18 (Rollo 25kg)", Brand = "Diaco", Category = "Hierro y acero", UnitOfMeasure = "rollo 25kg", UnitPrice = 145000m, Stock = 70, MinimumStock = 15, Description = "Alambre maleable de amarre para armaduras de hierro estructural." },
            new() { Sku = "HIE-005", Name = "Perfil Estructural en C 100x50x2mm (6m)", Brand = "Acesco", Category = "Hierro y acero", UnitOfMeasure = "perfil 6m", UnitPrice = 76000m, Stock = 120, MinimumStock = 25, Description = "Perfil metálico galvanizado para cubiertas, correas y cerramientos." },
            new() { Sku = "HIE-006", Name = "Tubo Estructural Cuadrado 3x3 Pulg Cal 14", Brand = "Acesco", Category = "Hierro y acero", UnitOfMeasure = "tubo 6m", UnitPrice = 98000m, Stock = 85, MinimumStock = 15, Description = "Tubería estructural para columnas ligeras y pórticos metálicos." },
            new() { Sku = "HIE-007", Name = "Ángulo Estructural 1-1/2 x 1/8 Pulgada (6m)", Brand = "Paz del Río", Category = "Hierro y acero", UnitOfMeasure = "unidad 6m", UnitPrice = 39000m, Stock = 160, MinimumStock = 30, Description = "Perfil angular de acero al carbono para cerrajería y herrería pesada." },

            // 5. Pinturas y recubrimientos (6 productos)
            new() { Sku = "PIN-001", Name = "Pintura Viniltex Blanco Almendra (Cuñete 5 Gal)", Brand = "Pintuco", Category = "Pinturas", UnitOfMeasure = "cuñete 5 gal", UnitPrice = 285000m, Stock = 65, MinimumStock = 10, Description = "Pintura lavable tipo 1 para interiores y cielorrasos residenciales." },
            new() { Sku = "PIN-002", Name = "Pintura Acrílica Fachada Koraza (Cuñete 5 Gal)", Brand = "Pintuco", Category = "Pinturas", UnitOfMeasure = "cuñete 5 gal", UnitPrice = 345000m, Stock = 45, MinimumStock = 10, Description = "Pintura antihumedad resistente a intemperie, rayos UV y polución." },
            new() { Sku = "PIN-003", Name = "Esmalte Sintético Anticorrosivo Gris (Galón)", Brand = "Corona", Category = "Pinturas", UnitOfMeasure = "galón", UnitPrice = 68000m, Stock = 85, MinimumStock = 15, Description = "Protección directa sobre superficies metálicas contra oxidación." },
            new() { Sku = "PIN-004", Name = "Pintura Epóxica para Pisos Tráfico Pesado (Galón)", Brand = "Sika", Category = "Pinturas", UnitOfMeasure = "kit galón", UnitPrice = 165000m, Stock = 40, MinimumStock = 8, Description = "Recubrimiento de alta resistencia química y mecánica para bodegas." },
            new() { Sku = "PIN-005", Name = "Masilla Acrílica para Drywall y Muros (Galón)", Brand = "Gyplac", Category = "Pinturas", UnitOfMeasure = "galón", UnitPrice = 26500m, Stock = 120, MinimumStock = 20, Description = "Masilla lista para tratamiento de juntas de yeso y resanes finos." },
            new() { Sku = "PIN-006", Name = "Sellador Acrílico Fijador Base Agua (5 Gal)", Brand = "Corona", Category = "Pinturas", UnitOfMeasure = "cuñete 5 gal", UnitPrice = 145000m, Stock = 50, MinimumStock = 10, Description = "Imprimante promotor de adherencia para muros nuevos de revoque." },

            // 6. Herramientas y maquinaria ligera (6 productos)
            new() { Sku = "HER-001", Name = "Rotomartillo SDS-Plus 800W Profesional", Brand = "DeWalt", Category = "Herramientas", UnitOfMeasure = "unidad", UnitPrice = 649000m, Stock = 25, MinimumStock = 5, Description = "Herramienta electro-neumática para perforación en concreto y demolición." },
            new() { Sku = "HER-002", Name = "Pulidora Angular 4-1/2 Pulgada 900W", Brand = "Stanley", Category = "Herramientas", UnitOfMeasure = "unidad", UnitPrice = 265000m, Stock = 40, MinimumStock = 8, Description = "Esmeriladora potente para corte de acero, bloque y desbaste." },
            new() { Sku = "HER-003", Name = "Carretilla de Construcción Antipinchazo 6ft³", Brand = "Bellota", Category = "Herramientas", UnitOfMeasure = "unidad", UnitPrice = 210000m, Stock = 35, MinimumStock = 6, Description = "Tolva de lámina reforzada calibre 16 con llanta de poliuretano sólido." },
            new() { Sku = "HER-004", Name = "Pala Redonda Herragro con Cabo Madera 44 pulg", Brand = "Herragro", Category = "Herramientas", UnitOfMeasure = "unidad", UnitPrice = 52000m, Stock = 90, MinimumStock = 15, Description = "Pala de acero templado para mezcla manual de concreto y excavación." },
            new() { Sku = "HER-005", Name = "Nivel Tubular de Aluminio Imantado 24 Pulg", Brand = "Stanley", Category = "Herramientas", UnitOfMeasure = "unidad", UnitPrice = 68000m, Stock = 50, MinimumStock = 10, Description = "Nivel de precisión con tres burbujas resistentes al impacto." },
            new() { Sku = "HER-006", Name = "Disco Diamantado Segmentado para Concreto 4.5 pulg", Brand = "DeWalt", Category = "Herramientas", UnitOfMeasure = "unidad", UnitPrice = 42000m, Stock = 180, MinimumStock = 30, Description = "Disco de corte rápido en seco o húmedo para mampostería y piedra." },

            // 7. Eléctricos para construcción (5 productos)
            new() { Sku = "ELE-001", Name = "Tablero Eléctrico de Distribución 8 Circuitos", Brand = "Schneider Electric", Category = "Eléctricos", UnitOfMeasure = "unidad", UnitPrice = 145000m, Stock = 30, MinimumStock = 5, Description = "Caja bifásica de sobreponer con barrajes de cobre certificado RETIE." },
            new() { Sku = "ELE-002", Name = "Cable de Cobre THHN Calibre 12 AWG (Rollo 100m)", Brand = "Celta", Category = "Eléctricos", UnitOfMeasure = "rollo 100m", UnitPrice = 265000m, Stock = 55, MinimumStock = 10, Description = "Conductor eléctrico de alta seguridad certificado para tomas y circuitos." },
            new() { Sku = "ELE-003", Name = "Cable de Cobre THHN Calibre 10 AWG (Rollo 100m)", Brand = "Celta", Category = "Eléctricos", UnitOfMeasure = "rollo 100m", UnitPrice = 398000m, Stock = 45, MinimumStock = 10, Description = "Cable de alimentación principal y circuitos de aire acondicionado." },
            new() { Sku = "ELE-004", Name = "Interruptor Termomagnético Mini 1x20A", Brand = "Schneider Electric", Category = "Eléctricos", UnitOfMeasure = "unidad", UnitPrice = 28500m, Stock = 210, MinimumStock = 30, Description = "Breaker monofásico de protección contra sobrecarga y cortocircuito." },
            new() { Sku = "ELE-005", Name = "Tubo Conduit PVC Eléctrico 1/2 Pulgada (3m)", Brand = "Pavco", Category = "Eléctricos", UnitOfMeasure = "tubo 3m", UnitPrice = 11200m, Stock = 400, MinimumStock = 60, Description = "Ducto liviano no propagador de llama para cableado empotrado." },

            // 8. Plomería e hidrosanitario (5 productos)
            new() { Sku = "PLO-001", Name = "Tubo PVC Presión RDE 21 1/2 Pulgada (6m)", Brand = "Pavco", Category = "Plomería", UnitOfMeasure = "tubo 6m", UnitPrice = 24500m, Stock = 320, MinimumStock = 50, Description = "Tubería certificada para conducción de agua potable a presión." },
            new() { Sku = "PLO-002", Name = "Tubo Sanitario PVC Desagüe 4 Pulgadas (6m)", Brand = "Pavco", Category = "Plomería", UnitOfMeasure = "tubo 6m", UnitPrice = 88000m, Stock = 140, MinimumStock = 20, Description = "Tubería de gran diámetro para bajantes de aguas negras y lluvias." },
            new() { Sku = "PLO-003", Name = "Tanque de Almacenamiento Agua 1000L Cilíndrico", Brand = "Colpozos", Category = "Plomería", UnitOfMeasure = "unidad", UnitPrice = 435000m, Stock = 18, MinimumStock = 4, Description = "Tanque bicapa antibacterial con filtro y válvula de llenado." },
            new() { Sku = "PLO-004", Name = "Válvula de Paso Bola en Bronce 1/2 Pulgada", Brand = "Grival", Category = "Plomería", UnitOfMeasure = "unidad", UnitPrice = 32000m, Stock = 110, MinimumStock = 20, Description = "Llave de corte de paso total con cuerpo metálico resistente a alta presión." },
            new() { Sku = "PLO-005", Name = "Soldadura PVC Pegamento Extra Fuerte (1/4 Gal)", Brand = "Pavco", Category = "Plomería", UnitOfMeasure = "lata 1/4 gal", UnitPrice = 42500m, Stock = 90, MinimumStock = 15, Description = "Cemento solvente para unión rígida de tuberías de presión y sanitarias." }
        };

        var added = 0;
        foreach (var prod in productCatalog)
        {
            if (!await context.Products.AnyAsync(p => p.Sku == prod.Sku))
            {
                prod.CreatedAtUtc = DateTime.UtcNow.AddDays(-60);
                context.Products.Add(prod);
                added++;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("  [Productos] Catálogo actualizado: agregados {Count} productos reales en 8 categorías.", added);
    }

    private static async Task SeedVehiclesAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingCount = await context.Vehicles.CountAsync();
        if (existingCount >= 15)
        {
            logger.LogInformation("  [Flota] Ya existen {Count} vehículos en base de datos. Omitiendo generación.", existingCount);
            return;
        }

        // 15 Vehículos y maquinaria pesada: 10 disponibles (0), 4 alquilados (1), 1 en mantenimiento (2)
        var fleet = new List<Vehicle>
        {
            // Disponibles (Status = Available = 0)
            new() { Plate = "FMR-101", Brand = "Caterpillar", Model = "Retroexcavadora 420F2 IT 4x4", Year = 2023, Type = VehicleType.HeavyMachinery, DailyRate = 750000m, LoadCapacity = "1.0 m³", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "FMR-102", Brand = "Bobcat", Model = "Minicargador S570 con Cucharón", Year = 2022, Type = VehicleType.HeavyMachinery, DailyRate = 480000m, LoadCapacity = "890 kg", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "FMR-104", Brand = "Bobcat", Model = "Miniexcavadora E35 Compacta", Year = 2024, Type = VehicleType.HeavyMachinery, DailyRate = 620000m, LoadCapacity = "3.5 Ton", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "CAT-201", Brand = "Caterpillar", Model = "Excavadora Hidráulica 320D Orugas", Year = 2023, Type = VehicleType.HeavyMachinery, DailyRate = 1250000m, LoadCapacity = "1.4 m³", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "CAT-202", Brand = "Caterpillar", Model = "Motoniveladora 140K", Year = 2021, Type = VehicleType.HeavyMachinery, DailyRate = 980000m, LoadCapacity = "14 ft hoja", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "KOM-301", Brand = "Komatsu", Model = "Cargador Frontal WA320-6", Year = 2023, Type = VehicleType.HeavyMachinery, DailyRate = 890000m, LoadCapacity = "3.0 m³", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "CHV-601", Brand = "Chevrolet", Model = "Camión FVR 1724 Chasis Corto", Year = 2024, Type = VehicleType.PickupTruck, DailyRate = 420000m, LoadCapacity = "10.5 Ton", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "HIN-701", Brand = "Hino", Model = "Camión Dutro Pro Estacas", Year = 2023, Type = VehicleType.PickupTruck, DailyRate = 340000m, LoadCapacity = "4.5 Ton", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "JMC-801", Brand = "JMC", Model = "Camión Carrying Plus Furgón", Year = 2022, Type = VehicleType.PickupTruck, DailyRate = 290000m, LoadCapacity = "3.2 Ton", Status = VehicleStatus.Available, IsActive = true },
            new() { Plate = "TRK-901", Brand = "TrailKing", Model = "Remolque Cama Baja 3 Ejes", Year = 2021, Type = VehicleType.Trailer, DailyRate = 550000m, LoadCapacity = "35.0 Ton", Status = VehicleStatus.Available, IsActive = true },

            // Alquilados actualmente (Status = Rented = 1) -> tendrán contratos activos
            new() { Plate = "FMR-103", Brand = "Kenworth", Model = "Volqueta Doble Troque T800 14m³", Year = 2022, Type = VehicleType.HeavyMachinery, DailyRate = 920000m, LoadCapacity = "14.0 m³", Status = VehicleStatus.Rented, IsActive = true },
            new() { Plate = "VOL-111", Brand = "International", Model = "Volqueta Sencilla WorkStar 7m³", Year = 2023, Type = VehicleType.HeavyMachinery, DailyRate = 580000m, LoadCapacity = "7.0 m³", Status = VehicleStatus.Rented, IsActive = true },
            new() { Plate = "MIN-222", Brand = "Case", Model = "Retroexcavadora 580N 4x4", Year = 2024, Type = VehicleType.HeavyMachinery, DailyRate = 720000m, LoadCapacity = "1.0 m³", Status = VehicleStatus.Rented, IsActive = true },
            new() { Plate = "REM-333", Brand = "Romarco", Model = "Remolque Tolva Granelera 2 Ejes", Year = 2022, Type = VehicleType.Trailer, DailyRate = 380000m, LoadCapacity = "22.0 Ton", Status = VehicleStatus.Rented, IsActive = true },

            // En Mantenimiento preventivo (Status = Maintenance = 2)
            new() { Plate = "BOB-401", Brand = "Bobcat", Model = "Compactador de Suelo Vibratorio", Year = 2021, Type = VehicleType.HeavyMachinery, DailyRate = 450000m, LoadCapacity = "3.0 Ton", Status = VehicleStatus.Maintenance, IsActive = true }
        };

        var added = 0;
        foreach (var v in fleet)
        {
            var existing = await context.Vehicles.FirstOrDefaultAsync(x => x.Plate == v.Plate);
            if (existing == null)
            {
                v.CreatedAtUtc = DateTime.UtcNow.AddDays(-120);
                context.Vehicles.Add(v);
                added++;
            }
            else
            {
                // Actualizar coherencia de estado si ya existía
                existing.Status = v.Status;
                existing.DailyRate = v.DailyRate;
                existing.Type = v.Type;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("  [Flota] Flota actualizada: {Count} vehículos registrados y alineados.", added);
    }

    private static async Task SeedSalesAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingCount = await context.Sales.CountAsync();
        if (existingCount >= 40)
        {
            logger.LogInformation("  [Ventas] Ya existen {Count} ventas en historial. Omitiendo generación.", existingCount);
            return;
        }

        var customers = await context.Customers.ToListAsync();
        var products = await context.Products.Where(p => p.IsActive && p.Stock > 10).ToListAsync();

        if (customers.Count == 0 || products.Count == 0)
        {
            logger.LogWarning("  [Ventas] No hay suficientes clientes o productos para generar ventas.");
            return;
        }

        var faker = new Faker("es");
        var salesToGenerate = 40 - existingCount;
        var created = 0;

        for (int i = 1; i <= salesToGenerate; i++)
        {
            var consecutive = existingCount + i;
            var externalRef = $"VTA-2026-{consecutive:D4}";

            // Verificar si ya existe este número consecutivo
            if (await context.Sales.AnyAsync(s => s.ExternalReference == externalRef))
                continue;

            var customer = faker.PickRandom(customers);
            var daysAgo = faker.Random.Number(1, 180);
            var saleDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-daysAgo), DateTimeKind.Utc);

            var itemsCount = faker.Random.Number(1, 4);
            var pickedProducts = faker.PickRandom(products, itemsCount).DistinctBy(p => p.Id).ToList();

            var sale = new Sale
            {
                CustomerId = customer.Id,
                ExternalReference = externalRef,
                Status = daysAgo > 3 ? "Entregado" : "Pagado",
                CreatedAtUtc = saleDate,
                TaxRate = 0.19m
            };

            decimal subtotal = 0m;
            foreach (var prod in pickedProducts)
            {
                var qty = faker.Random.Number(2, 20);
                var unitPrice = prod.UnitPrice;
                var lineTotal = unitPrice * qty;

                var detail = new SaleDetail
                {
                    SaleId = sale.Id,
                    ProductId = prod.Id,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                };

                sale.Details.Add(detail);
                subtotal += lineTotal;

                // Descontar inventario real
                if (prod.Stock >= qty)
                {
                    prod.Stock -= qty;
                }
            }

            sale.Subtotal = subtotal;
            sale.TaxAmount = Math.Round(subtotal * 0.19m, 2);
            sale.Total = subtotal + sale.TaxAmount;

            context.Sales.Add(sale);
            created++;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("  [Ventas] Generadas {Count} ventas históricas con inventario sincronizado e IVA 19%.", created);
    }

    private static async Task SeedRentalsAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingCount = await context.Rentals.CountAsync();
        if (existingCount >= 15)
        {
            logger.LogInformation("  [Alquileres] Ya existen {Count} contratos de alquiler. Omitiendo generación.", existingCount);
            return;
        }

        var customers = await context.Customers.ToListAsync();
        var vehicles = await context.Vehicles.ToListAsync();

        if (customers.Count == 0 || vehicles.Count == 0) return;

        var faker = new Faker("es");
        var created = 0;

        // 1. Alquileres activos para los vehículos con Status == Rented (1)
        var rentedVehicles = vehicles.Where(v => v.Status == VehicleStatus.Rented).ToList();
        foreach (var v in rentedVehicles)
        {
            var hasActiveRental = await context.Rentals.AnyAsync(r => r.VehicleId == v.Id && r.Status == RentalStatus.Active);
            if (!hasActiveRental)
            {
                var customer = faker.PickRandom(customers);
                var startDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-faker.Random.Number(2, 7)), DateTimeKind.Utc);
                var endDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(faker.Random.Number(5, 15)), DateTimeKind.Utc);
                var days = (int)Math.Ceiling((endDate - startDate).TotalDays);
                var subtotal = v.DailyRate * days;
                var taxAmount = Math.Round(subtotal * 0.19m, 2);

                var activeRental = new Rental
                {
                    CustomerId = customer.Id,
                    VehicleId = v.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysRented = days,
                    DailyRate = v.DailyRate,
                    Subtotal = subtotal,
                    TaxRate = 0.19m,
                    TaxAmount = taxAmount,
                    Total = subtotal + taxAmount,
                    Status = RentalStatus.Active,
                    Notes = $"Alquiler en operación activa en obra para {customer.FirstName} {customer.LastName}.",
                    CreatedAtUtc = startDate
                };

                context.Rentals.Add(activeRental);
                created++;
            }
        }

        // 2. Alquileres históricos completados para otros vehículos
        var otherVehicles = vehicles.Where(v => v.Status != VehicleStatus.Rented).ToList();
        var rentalsNeeded = 15 - (existingCount + created);

        for (int i = 0; i < rentalsNeeded && i < otherVehicles.Count; i++)
        {
            var v = otherVehicles[i];
            var customer = faker.PickRandom(customers);
            var daysAgo = faker.Random.Number(20, 150);
            var startDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-daysAgo), DateTimeKind.Utc);
            var days = faker.Random.Number(3, 10);
            var endDate = DateTime.SpecifyKind(startDate.AddDays(days), DateTimeKind.Utc);
            var subtotal = v.DailyRate * days;
            var taxAmount = Math.Round(subtotal * 0.19m, 2);

            var pastRental = new Rental
            {
                CustomerId = customer.Id,
                VehicleId = v.Id,
                StartDate = startDate,
                EndDate = endDate,
                ActualReturnDate = endDate,
                DaysRented = days,
                DailyRate = v.DailyRate,
                Subtotal = subtotal,
                TaxRate = 0.19m,
                TaxAmount = taxAmount,
                Total = subtotal + taxAmount,
                Status = RentalStatus.Completed,
                Notes = "Contrato completado a satisfacción y vehículo retornado.",
                CreatedAtUtc = startDate
            };

            context.Rentals.Add(pastRental);
            created++;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("  [Alquileres] Generados {Count} contratos de alquiler coherentes con los estados de flota.", created);
    }

    private static async Task PrintDatabaseSummaryAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var totalUsers = await userManager.Users.CountAsync();
        var totalCustomers = await context.Customers.CountAsync();
        var totalProducts = await context.Products.CountAsync();
        var totalVehicles = await context.Vehicles.CountAsync();
        var availableVehicles = await context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Available);
        var rentedVehicles = await context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Rented);
        var totalSales = await context.Sales.CountAsync();
        var totalDetails = await context.SaleDetails.CountAsync();
        var totalRentals = await context.Rentals.CountAsync();
        var activeRentals = await context.Rentals.CountAsync(r => r.Status == RentalStatus.Active);
        var categories = await context.Products.Select(p => p.Category).Distinct().ToListAsync();

        logger.LogInformation(@"
========================================================================
📊 RESUMEN FINAL DE BASE DE DATOS SEMILLA (FIRMEZA API)
========================================================================
 👤 Usuarios Identity:       {TotalUsers}
 🏢 Clientes / Empresas:     {TotalCustomers} (20+ contratistas vinculados a Identity)
 📦 Productos en Catálogo:   {TotalProducts}
 🏷️ Categorías Reales ({CatCount}): {Categories}
 🚜 Flota de Vehículos:      {TotalVehicles} (Disponibles: {AvailableVehicles}, Alquilados: {RentedVehicles})
 🧾 Ventas Registradas:      {TotalSales} ({TotalDetails} líneas de detalle)
 📋 Contratos de Alquiler:   {TotalRentals} (Activos en curso: {ActiveRentals})
========================================================================
 ✅ Estado: Todos los datos son coherentes y listos para producción / sustentación.
",
            totalUsers,
            totalCustomers,
            totalProducts,
            categories.Count,
            string.Join(", ", categories),
            totalVehicles,
            availableVehicles,
            rentedVehicles,
            totalSales,
            totalDetails,
            totalRentals,
            activeRentals);
    }
}
