# Firmeza.Api — Presentation / entrada HTTP

## Qué es este proyecto

`Firmeza.Api` es la capa de presentación del backend. Expone endpoints REST
mediante controladores Web API, configura autenticación, autorización,
validación HTTP, middleware, CORS y documentación OpenAPI/Scalar.

**Aclaración para la evaluación:** este repositorio backend no contiene vistas
Razor ni una interfaz MVC de páginas. Usa controladores de ASP.NET Core, pero
responde principalmente JSON/archivos para un cliente separado. La aplicación
cliente se encuentra en otro repositorio. Si el evaluador exige páginas MVC,
eso sería una presentación distinta que no está implementada aquí.

## Funcionalidades HTTP

Todos los controladores usan rutas bajo `/api`:

- **Auth**: `POST /api/Auth/register` registra una cuenta de Identity con rol
  `Cliente`, aprovisiona su perfil y envía un correo de bienvenida.
  `POST /api/Auth/login` valida credenciales y devuelve un JWT con expiración
  de ocho horas y roles; `GET /api/Auth/me` devuelve la identidad autenticada.
- **Products**: consulta pública de catálogo con búsqueda, categoría,
  disponibilidad, stock bajo y paginación; administradores crean, editan,
  eliminan y exportan el catálogo a PDF. Hay consultas de productos y conteo
  de stock bajo.
- **Vehicles**: consulta pública de flota por estado/tipo; administradores
  gestionan el CRUD de vehículos.
- **Customers**: consulta/edición de perfil propio; administradores gestionan
  clientes y pueden exportar un directorio PDF.
- **Sales**: consulta/cálculo de carrito, creación de venta, consulta y descarga
  del recibo PDF, cancelación administrativa.
- **Rentals**: cotización, solicitud, consulta y cancelación administrativa de
  alquileres.
- **Dashboard**: métricas agregadas solo para el rol `Administrador`.
- **Excel**: importación de productos/clientes y exportación de ventas,
  productos/clientes. Está restringido a administradores; las importaciones
  requieren `.xlsx` y limitan el archivo a 10 MB.

Los endpoints de ventas/alquileres restringen a cada cliente a sus propios
registros y obtienen su perfil a partir de los claims del JWT. No confían en un
`CustomerId` arbitrario enviado por el cliente para comprar o alquilar. Los
CRUDs y reportes administrativos requieren el rol `Administrador`.

## Identity y JWT en este backend

Identity está configurado en Infrastructure. `AuthController` usa
`UserManager<ApplicationUser>` para registrar/buscar usuarios, comprobar
contraseñas y consultar roles. En el login la API firma un JWT; el middleware
Bearer valida emisor, audiencia, firma y expiración en solicitudes posteriores.

En la práctica: Identity comprueba **quién es el usuario**; roles/claims
describen **qué permisos tiene**; JWT transporta esos claims entre el cliente y
la API. Aquí no se configura autenticación por cookies para vistas MVC.

## Arranque y pipeline

`Program.cs` registra `AddApplication()` y `AddInfrastructure(configuration)`,
configura CORS y JWT Bearer, agrega controladores con `FluentValidationFilter`,
y luego arma el pipeline. En orden general:

1. Middleware global de errores y encabezados de seguridad.
2. HTTPS y CORS.
3. Autenticación Bearer y autorización por atributos/roles.
4. Migraciones EF Core al iniciar (la opción `ApplyMigrationsOnStartup` se
   considera activada si no se configura).
5. Si es Development —o se habilita `SeedData:Enabled`—, inicialización de
   roles/datos.
6. Mapeo de controladores.

En Development también se publican OpenAPI y Scalar. Los validadores de
Application se ejecutan como filtro antes de la acción; errores de aplicación
no controlados se traducen a respuestas `ProblemDetails` por el middleware
global.

## Valores de configuración relevantes

La configuración base está en `appsettings.json` y puede sobrescribirse con
variables de entorno:

- `ConnectionStrings__DefaultConnection`: conexión PostgreSQL.
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`: firma y validación de tokens.
  La clave debe tener al menos 32 caracteres; en producción no debe quedar vacía.
- `Cors__AllowedOrigins__0`: origen permitido del cliente web.
- `Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `Smtp__From`:
  envío de notificaciones.
- `AdminUser__Email` y `AdminUser__Password`: aprovisionamiento opcional de
  administrador; guarda la contraseña como secreto.

Para entender quién lee cada valor, sigue `Program.cs` (JWT, CORS y migraciones),
`Infrastructure/DependencyInjection.cs` (PostgreSQL/Identity) y
`Infrastructure/Services/EmailService.cs` (SMTP).

## Dependencias entre capas y flujo de petición

```text
Cliente HTTP con Bearer JWT
  -> middleware de autenticación/autorización
  -> controlador de Firmeza.Api
  -> servicio/caso de uso de Application o interfaz de repositorio
  -> implementación de Infrastructure
  -> ApplicationDbContext / EF Core / Npgsql
  -> PostgreSQL
```

Los controladores reciben y responden HTTP; Application coordina casos de uso;
Infrastructure implementa persistencia e integraciones; Domain contiene los
conceptos del negocio. La inyección de dependencias conecta las interfaces con
sus implementaciones al iniciar la API.

Para ejecutar, desde la raíz usa `dotnet run --project src/Firmeza.Api`; revisa
primero que `ConnectionStrings:DefaultConnection` y `Jwt:Key` apunten a valores
locales/secrets apropiados. La API puede aplicar migraciones automáticamente
durante el arranque según la configuración indicada arriba.
