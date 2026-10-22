# Firmeza - Sistema de Gestión de Materiales de Construcción y Alquiler de Flota

Proyecto desarrollado en C# y .NET 10 bajo los principios de Clean Architecture Vanilla como solución backend Web API para la gestión integral de venta de materiales y alquiler de vehículos y maquinaria pesada.

## Descripción del Proyecto

Firmeza es una plataforma backend diseñada para centralizar dos líneas de negocio complementarias dentro del sector de la construcción:

1. **Venta y distribución de materiales de construcción:** control de catálogo de productos clasificados por marcas y SKU, control de inventario, detección automática de stock crítico, cálculo dinámico de carritos de compras con impuestos y facturación con generación de recibos PDF y notificaciones por correo.
2. **Alquiler de flota de vehículos y maquinaria:** gestión de camiones, maquinaria pesada y remolques, cotizador de costos por días de alquiler, control de estados de flota y prevención de solapamientos de fechas de reserva.

El sistema expone una API RESTful desacoplada consumida exclusivamente mediante peticiones HTTP por una aplicación cliente SPA desarrollada en Angular. La persistencia opera sobre PostgreSQL alojado en Supabase a través de Entity Framework Core, con seguridad administrada por ASP.NET Core Identity y tokens JWT.

---

## Diagramas de Arquitectura y Diseño

### 1. Diagrama de Arquitectura de Capas

```mermaid
graph TD
    Client[Cliente Angular SPA] -->|HTTP / JSON / JWT| API[Firmeza.Api]
    API --> Application[Firmeza.Application]
    API --> Infrastructure[Firmeza.Infrastructure]
    Infrastructure --> Application
    Infrastructure -->|Implementa| Domain[Firmeza.Domain]
    Application -->|Depende de| Domain
    Infrastructure -->|Npgsql / EF Core| Database[(PostgreSQL en Supabase)]
```

### 2. Diagrama Entidad-Relación (ER)

```mermaid
erDiagram
    Customers ||--o{ Sales : "realiza"
    Customers ||--o{ Rentals : "solicita"
    Sales ||--|{ SaleDetails : "contiene"
    Products ||--o{ SaleDetails : "incluido_en"
    Vehicles ||--o{ Rentals : "asignado_a"

    Customers {
        guid Id PK
        string FirstName
        string LastName
        string DocumentNumber UK
        string DocumentType
        string Email UK
        string Phone
        string Address
        int Age
        datetime CreatedAtUtc
    }

    Products {
        guid Id PK
        string Sku UK
        string Name
        string Brand
        string Category
        string UnitOfMeasure
        decimal UnitPrice
        int Stock
        int MinimumStock
        string Description
        boolean IsActive
        datetime CreatedAtUtc
    }

    Vehicles {
        guid Id PK
        string Plate UK
        string Brand
        string Model
        int Year
        int Type
        decimal DailyRate
        decimal LoadCapacity
        int Status
        boolean IsActive
        datetime CreatedAtUtc
    }

    Sales {
        guid Id PK
        guid CustomerId FK
        string Status
        string ExternalReference
        decimal Subtotal
        decimal TaxRate
        decimal TaxAmount
        decimal Total
        datetime CreatedAtUtc
    }

    SaleDetails {
        guid Id PK
        guid SaleId FK
        guid ProductId FK
        int Quantity
        decimal UnitPrice
        decimal LineTotal
        datetime CreatedAtUtc
    }

    Rentals {
        guid Id PK
        guid CustomerId FK
        guid VehicleId FK
        datetime StartDate
        datetime EndDate
        int DaysRented
        decimal DailyRate
        decimal Subtotal
        decimal TaxRate
        decimal TaxAmount
        decimal Total
        int Status
        string Notes
        datetime CreatedAtUtc
    }
```

### 3. Diagrama de Clases (Dominio y Servicios)

```mermaid
classDiagram
    class BaseEntity {
        +Guid Id
        +DateTime CreatedAtUtc
        +DateTime UpdatedAtUtc
    }

    class Customer {
        +string FirstName
        +string LastName
        +string DocumentNumber
        +string Email
        +string Phone
        +ICollection~Sale~ Sales
        +ICollection~Rental~ Rentals
    }

    class Product {
        +string Sku
        +string Name
        +string Brand
        +decimal UnitPrice
        +int Stock
        +int MinimumStock
    }

    class Vehicle {
        +string Plate
        +string Brand
        +string Model
        +decimal DailyRate
        +VehicleStatus Status
    }

    class Sale {
        +Guid CustomerId
        +string Status
        +decimal Subtotal
        +decimal Total
        +ICollection~SaleDetail~ Details
    }

    class Rental {
        +Guid CustomerId
        +Guid VehicleId
        +DateTime StartDate
        +DateTime EndDate
        +RentalStatus Status
        +decimal Total
    }

    class ISaleService {
        <<interface>>
        +CalculateCartAsync()
        +CreateSaleAsync()
        +GetSaleByIdAsync()
        +CancelSaleAsync()
        +GetSaleReceiptBytesAsync()
    }

    class IRentalService {
        <<interface>>
        +CalculateRentalQuoteAsync()
        +CreateRentalAsync()
        +GetRentalByIdAsync()
        +CancelRentalAsync()
    }

    BaseEntity <|-- Customer
    BaseEntity <|-- Product
    BaseEntity <|-- Vehicle
    BaseEntity <|-- Sale
    BaseEntity <|-- Rental
```

---

## Estructura del Proyecto

```text
Firmeza.API/
├── src/
│   ├── Firmeza.Domain/              # Entidades, Enums, Excepciones y Reglas puras
│   ├── Firmeza.Application/         # DTOs, Mappings, Interfaces, Servicios y FluentValidation
│   ├── Firmeza.Infrastructure/      # EF Core, Identity, Repositorios, ClosedXML, QuestPDF, MailKit
│   └── Firmeza.Api/                 # Controladores REST, Middlewares, Filtros, Scalar OpenAPI
├── tests/
│   └── Firmeza.Tests/               # Suite de pruebas unitarias xUnit + Moq
├── Dockerfile                       # Multi-stage build para imagen de producción de la API
├── Dockerfile.tests                 # Contenedor para ejecución automatizada de pruebas
├── docker-compose.yml               # Orquestación con pruebas previas y variables de entorno
└── README.md
```

---

## Requisitos del Sistema
- **.NET 10 SDK** (o versión 10.0+)
- **Node.js 20+** y **npm**
- **PostgreSQL 15/16** o instancia activa en **Supabase** (IPv4 Session Pooler)

---

## Configuración y Ejecución Local (Sin Docker)

### 1. Backend Web API (.NET 10)
1. Copiar la plantilla de configuración:
   ```bash
   cp src/Firmeza.Api/appsettings.Development.json.example src/Firmeza.Api/appsettings.Development.json
   ```
2. Completar la cadena de conexión a PostgreSQL en `src/Firmeza.Api/appsettings.Development.json`.
3. Aplicar migraciones pendientes (si se usa una base de datos nueva):
   ```bash
   dotnet ef database update --project src/Firmeza.Infrastructure --startup-project src/Firmeza.Api
   ```
4. Iniciar la API:
   ```bash
   cd src/Firmeza.Api
   dotnet run --launch-profile http
   ```
   * **URL de la API:** `http://localhost:5054`
   * **Documentación interactiva Scalar:** `http://localhost:5054/scalar/v1`

---

### 2. Frontend SPA (Angular 20 Standalone)
1. Abrir una nueva terminal e instalar dependencias:
   ```bash
   cd artyomalvarez-Firmeza.Web
   npm install
   ```
2. Iniciar el servidor de desarrollo:
   ```bash
   npm start
   ```
   * **URL del Portal Web:** `http://localhost:4200`

---

## Usuarios de Prueba Preconfigurados
- **Administrador del Sistema:**
  * Correo: `admin@firmeza.com`
  * Contraseña: `Admin123*Password`
  * Acceso al panel administrativo: `http://localhost:4200/admin`
- **Cliente Corporativo:**
  * Correo: `cliente@firmeza.com`
  * Contraseña: `Cliente2026*`
  * También es posible registrar clientes nuevos directamente desde `http://localhost:4200/registro`.

---

## Pruebas Unitarias Automatizadas (xUnit)
Para compilar y ejecutar las 29 pruebas unitarias de lógica de negocio:
```bash
dotnet test
```

---

## Despliegue con Docker (Opcional)
Los archivos de containerización (`Dockerfile`, `Dockerfile.tests`, `nginx.conf`, `.dockerignore` y `docker-compose.yml`) se encuentran completamente estructurados y verificados a nivel de configuración para su orquestación en equipos con Docker disponible:
```bash
docker compose up --build
```
> *Nota:* La suite de pruebas `tests` actúa como compuerta de calidad (`condition: service_completed_successfully`), garantizando que la API y el cliente solo inicien si todas las pruebas pasan.
