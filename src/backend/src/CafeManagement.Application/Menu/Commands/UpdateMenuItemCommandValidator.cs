using CafeManagement.Application.Menu.Commands;
using CafeManagement.Application.Common.Dtos;
using FluentValidation;

namespace CafeManagement.Application.Menu.Commands;

public class UpdateMenuItemCommandValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Menu item ID is required");

        RuleFor(x => x.Category)
            .MaximumLength(50).WithMessage("Category must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .NotNull()
            .WithMessage("Price is required")
            .DependentRules(() =>
            {
                RuleFor(x => x.Price!)
                    .SetValidator(new MoneyDtoValidator());
            });

        RuleForEach(x => x.Ingredients ?? Enumerable.Empty<string>())
            .MaximumLength(100).WithMessage("Ingredient name must not exceed 100 characters");

        RuleForEach(x => x.Allergens ?? Enumerable.Empty<string>())
            .MaximumLength(100).WithMessage("Allergen name must not exceed 100 characters");
    }
}

public class UpdateMenuItemAvailabilityCommandValidator : AbstractValidator<UpdateMenuItemAvailabilityCommand>
{
    public UpdateMenuItemAvailabilityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Menu item ID is required");
        RuleFor(x => x.Availability).NotNull().WithMessage("Availability is required");
    }
}

public class DeactivateMenuItemCommandValidator : AbstractValidator<DeactivateMenuItemCommand>
{
    public DeactivateMenuItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Menu item ID is required");
    }
}

public class MoneyDtoValidator : AbstractValidator<MoneyDto>
{
    public MoneyDtoValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Price must be greater than 0");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required").Length(3).WithMessage("Currency must be 3 characters");
    }
}
