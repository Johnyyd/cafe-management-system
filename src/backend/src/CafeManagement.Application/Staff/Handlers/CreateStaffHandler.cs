using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Staff.Commands;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Staff.Handlers;

public class CreateStaffHandler : IRequestHandler<CreateStaffCommand, Result<ObjectId>>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateStaffHandler> _logger;

    public CreateStaffHandler(
        IStaffRepository staffRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateStaffHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ObjectId>> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating staff with name: {FirstName} {LastName}", request.FirstName, request.LastName);

        try
        {
            // Check if staff with same email already exists
            var existingStaff = await _staffRepository.GetByEmailAsync(request.Contact.Email, cancellationToken);
            if (existingStaff != null)
            {
                return Result.Fail(DomainErrors.General.AlreadyExists("Staff", "Email", request.Contact.Email));
            }

            // Create domain object
            var contact = new ContactInfo(
                request.Contact.Phone,
                request.Contact.Email);

            var staffResult = Domain.Staff.Staff.Hire(
                request.FirstName,
                request.LastName,
                request.Role,
                contact,
                request.HireDate,
                request.ShopId);

            if (staffResult.IsFailed)
            {
                return Result.Fail(staffResult.Errors);
            }

            var staff = staffResult.Value;

            // Save to repository
            await _staffRepository.AddAsync(staff, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Staff created successfully with Id: {StaffId}", staff.Id);

            return Result.Ok(staff.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff");
            return Result.Fail(DomainErrors.General.InvalidOperation(ex.Message));
        }
    }
}