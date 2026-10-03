using FluentValidation;

namespace CafeManagement.Application.Shops.Commands;

public class CreateShopCommandValidator : AbstractValidator<CreateShopCommand>
{
    public CreateShopCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required");

        RuleFor(x => x.Contact)
            .NotNull().WithMessage("Contact is required");

        RuleForEach(x => x.OperatingHours)
            .ChildRules(hours =>
            {
                hours.RuleFor(h => h.DayOfWeek)
                    .InclusiveBetween(0, 6).WithMessage("DayOfWeek must be between 0 (Sunday) and 6 (Saturday)");
                hours.RuleFor(h => h.OpenTime)
                    .LessThan(h => h.CloseTime).WithMessage("OpenTime must be before CloseTime");
            });
    }
}