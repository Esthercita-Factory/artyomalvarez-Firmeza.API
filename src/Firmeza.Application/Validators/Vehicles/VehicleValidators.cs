using FluentValidation;
using Firmeza.Application.DTOs.Vehicles;

namespace Firmeza.Application.Validators.Vehicles;

public class CreateVehicleDtoValidator : AbstractValidator<CreateVehicleDto>
{
    public CreateVehicleDtoValidator()
    {
        RuleFor(x => x.Plate)
            .NotEmpty().WithMessage("La placa del vehículo es obligatoria.")
            .MaximumLength(20).WithMessage("La placa no debe superar 20 caracteres.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca es obligatoria.")
            .MaximumLength(50).WithMessage("La marca no debe superar 50 caracteres.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo es obligatorio.")
            .MaximumLength(50).WithMessage("El modelo no debe superar 50 caracteres.");

        RuleFor(x => x.DailyRate)
            .GreaterThan(0).WithMessage("La tarifa diaria de alquiler debe ser mayor a 0.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1990, DateTime.UtcNow.Year + 1).WithMessage("El año del vehículo no es válido.");

        RuleFor(x => x.LoadCapacity)
            .GreaterThanOrEqualTo(0.ToString()).WithMessage("La capacidad de carga no puede ser negativa.");
    }
}

public class UpdateVehicleDtoValidator : AbstractValidator<UpdateVehicleDto>
{
    public UpdateVehicleDtoValidator()
    {
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca es obligatoria.")
            .MaximumLength(50).WithMessage("La marca no debe superar 50 caracteres.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo es obligatorio.")
            .MaximumLength(50).WithMessage("El modelo no debe superar 50 caracteres.");

        RuleFor(x => x.DailyRate)
            .GreaterThan(0).WithMessage("La tarifa diaria de alquiler debe ser mayor a 0.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1990, DateTime.UtcNow.Year + 1).WithMessage("El año del vehículo no es válido.");

        RuleFor(x => x.LoadCapacity)
            .GreaterThanOrEqualTo(0.ToString()).WithMessage("La capacidad de carga no puede ser negativa.");
    }
}
