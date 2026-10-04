using CafeManagement.Application.Staff.Commands;
using CafeManagement.Application.Common.Dtos;
using FluentValidation;

namespace CafeManagement.Application.Staff.Commands;

public class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Staff ID is required");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");

        RuleFor(x => x.Contact)
            .NotNull().WithMessage("Contact is required")
            .SetValidator(new ContactInfoDtoValidator());
    }
}

public class UpdateStaffRoleCommandValidator : AbstractValidator<UpdateStaffRoleCommand>
{
    public UpdateStaffRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Staff ID is required");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Invalid staff role");
    }
}

public class UpdateStaffEmploymentStatusCommandValidator : AbstractValidator<UpdateStaffEmploymentStatusCommand>
{
    public UpdateStaffEmploymentStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Staff ID is required");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Invalid employment status");
    }
}

public class TransferStaffCommandValidator : AbstractValidator<TransferStaffCommand>
{
    public TransferStaffCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Staff ID is required");
        RuleFor(x => x.ShopId).NotEmpty().WithMessage("Target shop ID is required");
    }
}

public class DeactivateStaffCommandValidator : AbstractValidator<DeactivateStaffCommand>
{
    public DeactivateStaffCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Staff ID is required");
    }
}

// Reuse the existing validator for ContactInfoDto
public class ContactInfoDtoValidator : AbstractValidator<ContactInfoDto>
{
    public ContactInfoDtoValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Matches(@"^[\d\s\-\+\(\)]{10,15}$").WithMessage("Invalid phone number format");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
