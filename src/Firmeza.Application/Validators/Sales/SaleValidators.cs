using FluentValidation;
using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Validators.Sales;

public class CreateSaleDtoValidator : AbstractValidator<CreateSaleDto>
{
    public CreateSaleDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0m, 1m).WithMessage("La tasa de impuesto debe estar entre 0% y 100%.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("El carrito debe contener al menos un producto.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("El ID del producto es obligatorio.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
        });
    }
}

public class CartItemDtoValidator : AbstractValidator<CartItemDto>
{
    public CartItemDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("El ID del producto es obligatorio.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
    }
}
