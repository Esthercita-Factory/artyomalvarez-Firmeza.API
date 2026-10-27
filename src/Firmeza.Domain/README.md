# Firmeza.Domain — reglas y modelo del negocio

## Para qué sirve esta capa

`Domain` representa el negocio de Firmeza sin depender de ASP.NET Core, EF Core,
PostgreSQL, Identity ni de la interfaz HTTP. Aquí se encuentran las entidades,
los estados del negocio y las excepciones propias del dominio.

La idea para explicarlo en una evaluación: **Domain define qué conceptos existen
en el negocio; no decide cómo se reciben por HTTP ni cómo se guardan**.

## Qué conceptos contiene Firmeza

- **Customer**: cliente con nombres, documento, correo y datos de contacto.
  Puede relacionarse con sus ventas y alquileres. `UserId` guarda el identificador
  del usuario de Identity como texto, pero Domain no referencia ni hereda de
  clases de Identity.
- **Product**: material para la venta, con SKU, marca, categoría, unidad, precio,
  inventario, stock mínimo y estado activo. `IsLowStock` calcula si el producto
  activo está en o por debajo de su mínimo.
- **Vehicle**: vehículo o maquinaria de la flota, con placa, marca, modelo, tipo,
  tarifa diaria, capacidad, estado y disponibilidad.
- **Sale** y **SaleDetail**: una venta pertenece a un cliente y contiene líneas
  con producto, cantidad, precio y total. La venta registra subtotal, impuesto,
  total, estado y referencia externa opcional.
- **Rental**: relaciona un cliente con un vehículo e incluye fechas, días,
  tarifas, impuesto, total, notas y estado.

Las entidades principales heredan de `BaseEntity`, que asigna un `Guid` de
versión 7 y guarda `CreatedAtUtc` y `UpdatedAtUtc`.

## Estados y reglas

Los enums definen tipos y estados para vehículos y alquileres: por ejemplo,
`VehicleType`, `VehicleStatus` y `RentalStatus`. `SaleStatus` también existe,
aunque `Sale.Status` actualmente está declarado como texto, no como ese enum.

En el código actual, varias reglas del proceso están implementadas en
`Application/Services/ApplicationServices.cs`, no como métodos de las entidades:
calcular una venta, restar/devolver inventario, cotizar un alquiler y comprobar
solapamientos. Es mejor describir la arquitectura tal como está, sin afirmar
que cada entidad es un agregado DDD con todas sus invariantes encapsuladas.

`DomainException` representa errores de negocio; `Error` y `BaseEvent` son tipos
comunes disponibles para el dominio.

## Qué no debe ir aquí

- Controladores, DTOs HTTP, configuración, JWT o código de Identity.
- `DbContext`, consultas EF Core, repositorios concretos o cadenas de conexión.
- QuestPDF, Excel, correo o detalles de PostgreSQL.

**Regla de dependencias:** Domain no depende de los otros proyectos. Application
puede usar Domain; Infrastructure y Api también lo conocen indirectamente a
través de sus referencias.

## Archivos para ubicarte

- `Entities/`: `Customer`, `Product`, `Vehicle`, `Sale`, `SaleDetail` y `Rental`.
- `Enums/`: tipos y estados de vehículos, ventas y alquileres.
- `Common/BaseEntity.cs`: identificador y fechas comunes.
- `Exceptions/DomainException.cs` y `Errors/Error.cs`: errores del dominio.
- `Events/BaseEvent.cs`: tipo base para eventos de dominio.

## Pregunta útil para defenderlo

> Si cambia PostgreSQL por otro proveedor, ¿tendrían que cambiar estas entidades?

No deberían: el modelo del negocio no depende del proveedor de persistencia.
