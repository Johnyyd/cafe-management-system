using CafeManagement.Application.Staff.Commands;
using FluentValidation;

namespace CafeManagement.Application.Staff.Commands;

public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required")
            .MaximumLength(50).WithMessage("FirstName must not exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required")
            .MaximumLength(50).WithMessage("LastName must not exceed 50 characters");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid StaffRole");

        RuleFor(x => x.Contact)
            .NotNull().WithMessage("Contact is required");

        RuleFor(x => x.HireDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("HireDate cannot be in the future");

        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("ShopId is required");
    }
}