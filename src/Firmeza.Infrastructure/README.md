# Firmeza.Infrastructure — persistencia e integraciones

## Para qué sirve esta capa

`Infrastructure` contiene los detalles técnicos que Application abstrae:
Entity Framework Core, PostgreSQL, Identity, repositorios concretos,
migraciones y conexiones con servicios de archivos, correo y Excel/PDF.

En una analogía, Application pide “guarda esta venta” mediante un contrato;
Infrastructure es quien sabe cómo hacerlo con EF Core y PostgreSQL.

## Base de datos e Identity

- `Persistence/ApplicationDbContext.cs` hereda de
  `IdentityDbContext<ApplicationUser>`. Por eso el mismo contexto contiene las
  tablas de usuarios/roles de Identity y los `DbSet` de productos, clientes,
  vehículos, alquileres, ventas y detalles.
- `Identity/ApplicationUser.cs` extiende `IdentityUser` sin añadir propiedades
  propias actualmente. Las cuentas y los clientes del negocio son conceptos
  distintos: `Customer.UserId` enlaza al usuario autenticado.
- `DependencyInjection.cs` configura `UseNpgsql`, reintentos ante ciertos
  fallos de conexión, Identity Core con roles y los almacenes EF Core.
- `Persistence/Repositories/` implementa los contratos de Application con
  consultas EF Core. Incluye búsquedas, filtros, paginación, métricas y
  detección de alquileres solapados.
- `Repositories/UnitOfWork.cs` persiste los cambios compartidos del contexto
  mediante `SaveChangesAsync`.
- `Persistence/Migrations/` contiene las migraciones existentes y el snapshot
  del modelo EF Core.

La relación es: **Identity administra cuentas y roles; EF Core persiste sus
tablas; Npgsql es el proveedor de EF Core para PostgreSQL**. Identity no es una
base de datos y el token JWT no sustituye las tablas de usuarios.

La política de contraseña configurada en `AddInfrastructure` requiere al menos
6 caracteres, un dígito, una minúscula y una mayúscula; no exige un carácter
especial. También exige correo único. Los roles que usa la API son
`Administrador` y `Cliente`.

## Integraciones externas

- `Services/PdfService.cs` genera recibos de venta y reportes PDF de productos y
  clientes con QuestPDF.
- `Services/ExcelService.cs` importa productos/clientes y exporta ventas,
  productos y clientes a `.xlsx` con ClosedXML.
- `Services/EmailService.cs` envía notificaciones con MailKit y la configuración
  SMTP. Si no hay host SMTP, registra una advertencia y simula omitir el envío.
- `Persistence/Seeds/` prepara roles y datos iniciales; el usuario administrador
  solo se crea cuando se configura su contraseña. `DataSeeder` contiene también
  datos de prueba.

`AddInfrastructure(configuration)` registra en el contenedor el contexto,
Identity, repositorios y servicios externos. La Api llama ese método al iniciar.

## Crear o aplicar migraciones

Desde la raíz de la solución, el proyecto que contiene el contexto es
Infrastructure y el ejecutable que carga `Program.cs` y configuración es Api:

```bash
dotnet ef migrations add NombreDescriptivo \
  --project src/Firmeza.Infrastructure \
  --startup-project src/Firmeza.Api \
  --context ApplicationDbContext

dotnet ef database update \
  --project src/Firmeza.Infrastructure \
  --startup-project src/Firmeza.Api \
  --context ApplicationDbContext
```

`migrations add` genera archivos de migración en Infrastructure; `database
update` aplica las migraciones a la base indicada por la configuración de Api.
El proyecto de inicio debe poder construir el contexto y leer la conexión.

## Configuración al clonar

El proveedor de base de datos de este repositorio es PostgreSQL, también cuando
se despliega en Supabase. Para ejecutar la API directamente, .NET puede leer
variables como `ConnectionStrings__DefaultConnection` y `Jwt__Key` (los dos
guiones bajos representan `:` en las claves de configuración). Para Docker,
`docker-compose.yml` mapea valores del archivo `.env` a esas claves.

No reutilices contraseñas ni claves de ejemplo en un entorno real. Usa
User Secrets o variables de entorno para credenciales; no subas `.env` con
secretos. El proyecto Api configura su inicio de desarrollo con User Secrets.

## Paquetes que explican esta capa

- `Microsoft.EntityFrameworkCore`: ORM y contexto.
- `Npgsql.EntityFrameworkCore.PostgreSQL`: proveedor PostgreSQL de EF Core.
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: tablas/almacenamiento de
  Identity con EF Core.
- `ClosedXML`, `QuestPDF` y `MailKit`: Excel, PDF y correo respectivamente.

La versión objetivo de los proyectos es .NET 10; revisa compatibilidad de las
versiones de EF Core, Identity y Npgsql al actualizarlas.
