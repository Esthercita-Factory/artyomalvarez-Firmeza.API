using FluentValidation;
using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.DTOs.Vehicles;

namespace Firmeza.Application.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50).WithMessage("El SKU es obligatorio y no debe superar 50 caracteres.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithMessage("El nombre del producto es obligatorio.");
        RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("El precio unitario debe ser mayor a 0.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");
    }
}

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).WithMessage("El apellido es obligatorio.");
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(50).WithMessage("El número de documento es obligatorio.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
    }
}

public class CreateVehicleDtoValidator : AbstractValidator<CreateVehicleDto>
{
    public CreateVehicleDtoValidator()
    {
        RuleFor(x => x.Plate).NotEmpty().MaximumLength(20).WithMessage("La placa del vehículo es obligatoria.");
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(50).WithMessage("La marca es obligatoria.");
        RuleFor(x => x.Model).NotEmpty().MaximumLength(50).WithMessage("El modelo es obligatorio.");
        RuleFor(x => x.DailyRate).GreaterThan(0).WithMessage("La tarifa diaria de alquiler debe ser mayor a 0.");
        RuleFor(x => x.Year).InclusiveBetween(1990, DateTime.UtcNow.Year + 1).WithMessage("El año del vehículo no es válido.");
    }
}

public class CreateSaleDtoValidator : AbstractValidator<CreateSaleDto>
{
    public CreateSaleDtoValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("El cliente es obligatorio.");
        RuleFor(x => x.TaxRate).InclusiveBetween(0m, 1m).WithMessage("La tasa de impuesto debe estar entre 0% y 100%.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("El carrito debe contener al menos un producto.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("El ID del producto es obligatorio.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
        });
    }
}

public class CreateRentalDtoValidator : AbstractValidator<CreateRentalDto>
{
    public CreateRentalDtoValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("El cliente es obligatorio.");
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("El vehículo es obligatorio.");
        RuleFor(x => x.StartDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("La fecha de inicio no puede ser en el pasado.");
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");
    }
}
