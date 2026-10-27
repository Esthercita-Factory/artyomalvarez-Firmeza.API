# Firmeza.Application — casos de uso y contratos

## Para qué sirve esta capa

`Application` coordina operaciones del sistema. Conoce las reglas y entidades de
`Domain`, y define contratos para que otras capas proporcionen persistencia o
servicios externos. No debería conocer controladores, HTTP, EF Core ni
PostgreSQL.

Una analogía: si Domain describe las piezas y reglas del negocio, Application es
la persona que coordina una tarea usando esas piezas y solicita ayuda a través
de interfaces.

## Funcionalidades que coordina

- **Ventas** (`SaleService`): calcula carrito, subtotal, impuesto y total;
  crea ventas, valida stock disponible, descuenta inventario, consulta y cancela
  ventas, restaura existencias al cancelar y obtiene el recibo PDF. Después de
  guardar la venta intenta generar/guardar el recibo y enviar un correo.
- **Alquileres** (`RentalService`): cotiza por vehículo, fechas y tarifa diaria;
  calcula días e impuesto; al reservar valida que el vehículo esté disponible y
  que no exista otro alquiler con fechas solapadas; cambia el estado de la flota
  y guarda la reserva. Al cancelar, marca el alquiler cancelado y devuelve el
  vehículo a disponible.
- **Dashboard** (`DashboardService`): reúne ingresos y cantidades de ventas y
  alquileres activos, clientes, productos con stock bajo y vehículos disponibles.

Las operaciones anteriores se implementan en
`Services/ApplicationServices.cs`; sus contratos están en
`Interfaces/Services/IAppServices.cs`.

## Contratos de persistencia y servicios externos

- `Interfaces/Persistence/`: contratos de repositorio para clientes, productos,
  vehículos, ventas y alquileres, más `IUnitOfWork` para guardar cambios.
- `Interfaces/External/`: contratos de email, Excel y PDF.

Los contratos permiten que Application pida, por ejemplo, `IProductRepository`
sin conocer la clase concreta que usa EF Core. Infrastructure implementa esos
contratos y los registra en inyección de dependencias.

## DTOs, validación y mapeo

- `Dtos/`: formas de entrada y salida para clientes, productos, ventas,
  alquileres, vehículos, dashboard e importación/exportación Excel.
- `Validators/`: reglas de validación de los DTOs.
- `Common/Mappings/MappingProfile.cs`: conversiones entre DTOs y entidades con
  AutoMapper.
- `Common/Exceptions/`: errores de aplicación, como recurso inexistente o datos
  inválidos.
- `DependencyInjection.cs`: registra servicios de casos de uso, validadores y
  mapeos mediante `AddApplication()`.

**Importante sobre el código actual:** los casos de uso de ventas, alquileres y
dashboard sí están en Application. Parte del CRUD de productos, vehículos y
clientes se coordina directamente desde sus controladores usando interfaces de
repositorio; no todo el CRUD está encapsulado en servicios de Application.

## Flujo representativo

`SalesController` recibe el DTO → `ISaleService` coordina el caso de uso →
`IProductRepository` y `ICustomerRepository` consultan datos → `IUnitOfWork`
guarda los cambios → el resultado se convierte en `SaleDto`.

Application expresa la operación y sus dependencias; Infrastructure determina
cómo se consultan la base de datos o los servicios externos.
