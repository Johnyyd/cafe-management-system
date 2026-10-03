using CafeManagement.Application.Menu.Commands;
using FluentValidation;

namespace CafeManagement.Application.Menu.Commands;

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("ShopId is required");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(50).WithMessage("Category must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Price)
            .NotNull().WithMessage("Price is required");

        RuleFor(x => x.Price.Amount)
            .GreaterThan(0).WithMessage("Price must be greater than zero");

        RuleFor(x => x.Availability)
            .NotNull().WithMessage("Availability is required");

        RuleForEach(x => x.Availability.DaysOfWeek)
            .InclusiveBetween(0, 6).WithMessage("DaysOfWeek must be between 0 (Sunday) and 6 (Saturday)");
    }
}