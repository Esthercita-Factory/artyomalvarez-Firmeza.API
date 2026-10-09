using FluentValidation;
using Firmeza.Application.DTOs.Rentals;

namespace Firmeza.Application.Validators.Rentals;

public class CreateRentalDtoValidator : AbstractValidator<CreateRentalDto>
{
    public CreateRentalDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("El vehículo es obligatorio.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("La fecha de inicio no puede ser en el pasado.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");
    }
}
