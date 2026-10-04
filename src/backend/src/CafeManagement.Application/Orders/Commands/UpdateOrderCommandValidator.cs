using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Common.Dtos;
using FluentValidation;

namespace CafeManagement.Application.Orders.Commands;

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Order ID is required");
        RuleFor(x => x.CustomerInfo)
            .SetValidator(new CustomerInfoDtoNullableValidator())
            .When(x => x.CustomerInfo != null);
    }
}

public class CustomerInfoDtoNullableValidator : AbstractValidator<CustomerInfoDto?>
{
    public CustomerInfoDtoNullableValidator()
    {
        RuleFor(x => x).NotNull().WithMessage("CustomerInfo is required");
        When(x => x is not null, () =>
        {
            RuleFor(x => x!).SetValidator(new CustomerInfoDtoValidator());
        });
    }
}

public class CustomerInfoDtoValidator : AbstractValidator<CustomerInfoDto>
{
    public CustomerInfoDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Customer name is required").MaximumLength(100);
        RuleFor(x => x.Contact).MaximumLength(50);
        RuleFor(x => x.Type).IsInEnum();
    }
}

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Order ID is required");
    }
}

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Order ID is required");
        RuleFor(x => x.PaymentMethod).IsInEnum().WithMessage("Invalid payment method");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than 0");
    }
}

public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Order ID is required");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Invalid order status");
    }
}

public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
        RuleFor(x => x.MenuItemId).NotEmpty().WithMessage("Menu item ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}

public class UpdateOrderItemCommandValidator : AbstractValidator<UpdateOrderItemCommand>
{
    public UpdateOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
        RuleFor(x => x.OrderItemId).NotEmpty().WithMessage("Order item ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}

public class RemoveOrderItemCommandValidator : AbstractValidator<RemoveOrderItemCommand>
{
    public RemoveOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
        RuleFor(x => x.OrderItemId).NotEmpty().WithMessage("Order item ID is required");
    }
}