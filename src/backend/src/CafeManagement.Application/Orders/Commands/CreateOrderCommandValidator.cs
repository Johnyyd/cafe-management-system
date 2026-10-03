using CafeManagement.Application.Orders.Commands;
using FluentValidation;

namespace CafeManagement.Application.Orders.Commands;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("ShopId is required");

        RuleFor(x => x.StaffId)
            .NotEmpty().WithMessage("StaffId is required");

        RuleFor(x => x.CustomerInfo)
            .NotNull().WithMessage("CustomerInfo is required");

        RuleFor(x => x.CustomerInfo.Name)
            .NotEmpty().WithMessage("Customer name is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.MenuItemId)
                    .NotEmpty().WithMessage("MenuItemId is required");
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than zero");
                item.RuleFor(i => i.UnitPrice.Amount)
                    .GreaterThan(0).WithMessage("UnitPrice must be greater than zero");
            });
    }
}