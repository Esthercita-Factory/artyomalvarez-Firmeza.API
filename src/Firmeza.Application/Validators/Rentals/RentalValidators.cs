using FluentValidation;
using Firmeza.Application.DTOs.Rentals;

namespace Firmeza.Application.Validators.Rentals;

public class CreateRentalDtoValidator : AbstractValidator<CreateRentalDto>
{
    public CreateRentalDtoValidator()
    {
        // NOTA: CustomerId es opcional para el rol Cliente (se infiere de forma segura del JWT en RentalsController
        // para prevenir IDOR). Para administradores, se valida en el controlador si no se proporciona.
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("El vehículo es obligatorio.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date.AddDays(-1))
            .WithMessage("La fecha de inicio no puede ser anterior a hoy.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");
    }
}
