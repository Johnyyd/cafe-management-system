using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Staff.Commands;
using CafeManagement.Application.Staff.Queries;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Staff.Handlers;

public class UpdateStaffHandler : IRequestHandler<UpdateStaffCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateStaffHandler> _logger;

    public UpdateStaffHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<UpdateStaffHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateStaffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var contact = new ContactInfo(request.Contact.Phone, request.Contact.Email);

        var result = staff.Update(request.FirstName, request.LastName, contact);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff updated: {StaffId}", staff.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateStaffRoleHandler : IRequestHandler<UpdateStaffRoleCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateStaffRoleHandler> _logger;

    public UpdateStaffRoleHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<UpdateStaffRoleHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateStaffRoleCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var result = staff.ChangeRole(request.Role);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff role updated: {StaffId}", staff.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateStaffEmploymentStatusHandler : IRequestHandler<UpdateStaffEmploymentStatusCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateStaffEmploymentStatusHandler> _logger;

    public UpdateStaffEmploymentStatusHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<UpdateStaffEmploymentStatusHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateStaffEmploymentStatusCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var result = staff.ChangeEmploymentStatus(request.Status);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff employment status updated: {StaffId}", staff.Id);
        return FluentResults.Result.Ok();
    }
}

public class TransferStaffHandler : IRequestHandler<AssignStaffToShopCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransferStaffHandler> _logger;

    public TransferStaffHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<TransferStaffHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(AssignStaffToShopCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var result = staff.AssignToShop(request.ShopId, request.IsPrimary);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff assigned to shop: {StaffId} -> {ShopId}", staff.Id, request.ShopId);
        return FluentResults.Result.Ok();
    }
}

public class UnassignStaffFromShopHandler : IRequestHandler<UnassignStaffFromShopCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnassignStaffFromShopHandler> _logger;

    public UnassignStaffFromShopHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<UnassignStaffFromShopHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UnassignStaffFromShopCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var result = staff.UnassignFromShop(request.ShopId);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff unassigned from shop: {StaffId} -> {ShopId}", staff.Id, request.ShopId);
        return FluentResults.Result.Ok();
    }
}

public class SetStaffPrimaryShopHandler : IRequestHandler<SetStaffPrimaryShopCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetStaffPrimaryShopHandler> _logger;

    public SetStaffPrimaryShopHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<SetStaffPrimaryShopHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(SetStaffPrimaryShopCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var result = staff.SetPrimaryShop(request.ShopId);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff primary shop set: {StaffId} -> {ShopId}", staff.Id, request.ShopId);
        return FluentResults.Result.Ok();
    }
}

public class DeactivateStaffHandler : IRequestHandler<DeactivateStaffCommand, FluentResults.Result>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateStaffHandler> _logger;

    public DeactivateStaffHandler(IStaffRepository staffRepository, IUnitOfWork unitOfWork, ILogger<DeactivateStaffHandler> logger)
    {
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(DeactivateStaffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var result = staff.ChangeEmploymentStatus(EmploymentStatus.Terminated);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _staffRepository.UpdateAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff deactivated: {StaffId}", staff.Id);
        return FluentResults.Result.Ok();
    }
}

public class GetStaffHandler : IRequestHandler<GetStaffQuery, Result<PagedResult<StaffDto>>>
{
    private readonly IStaffRepository _staffRepository;

    public GetStaffHandler(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public async Task<Result<PagedResult<StaffDto>>> Handle(GetStaffQuery request, CancellationToken cancellationToken)
    {
        var staffList = await _staffRepository.ListAsync(cancellationToken);

        if (request.ShopId.HasValue && request.ShopId.Value != ObjectId.Empty)
            staffList = staffList.Where(s => s.ShopAssignments.Any(sa => sa.ShopId == request.ShopId.Value && sa.UnassignedDate == null)).ToList();

        if (request.Role.HasValue)
            staffList = staffList.Where(s => s.Role == request.Role.Value).ToList();

        if (request.Status.HasValue)
            staffList = staffList.Where(s => s.EmploymentStatus == request.Status.Value).ToList();

        var totalCount = staffList.Count;
        var pagedStaff = staffList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StaffDto(
                s.Id,
                s.FirstName,
                s.LastName,
                s.Role,
                new ContactInfoDto(s.Contact.Phone, s.Contact.Email),
                s.EmploymentStatus,
                s.HireDate,
                s.ShopAssignments.Select(sa => new StaffShopAssignmentDto(
                    sa.Id,
                    sa.StaffId,
                    sa.ShopId,
                    sa.AssignedDate,
                    sa.UnassignedDate,
                    sa.IsPrimary
                )).ToList(),
                s.CreatedAt,
                s.UpdatedAt,
                s.DeletedAt))
            .ToList();

        var result = new PagedResult<StaffDto>(pagedStaff, totalCount, request.Page, request.PageSize);
        return Result.Ok(result);
    }
}

public class GetStaffByIdHandler : IRequestHandler<GetStaffByIdQuery, Result<StaffDto>>
{
    private readonly IStaffRepository _staffRepository;

    public GetStaffByIdHandler(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public async Task<Result<StaffDto>> Handle(GetStaffByIdQuery request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.Id, cancellationToken);
        if (staff == null)
            return Result.Fail(DomainErrors.General.NotFound("Staff", request.Id));

        var dto = new StaffDto(
            staff.Id,
            staff.FirstName,
            staff.LastName,
            staff.Role,
            new ContactInfoDto(staff.Contact.Phone, staff.Contact.Email),
            staff.EmploymentStatus,
            staff.HireDate,
            staff.ShopAssignments.Select(sa => new StaffShopAssignmentDto(
                sa.Id,
                sa.StaffId,
                sa.ShopId,
                sa.AssignedDate,
                sa.UnassignedDate,
                sa.IsPrimary
            )).ToList(),
            staff.CreatedAt,
            staff.UpdatedAt,
            staff.DeletedAt);

        return Result.Ok(dto);
    }
}

public class GetStaffByShopHandler : IRequestHandler<GetStaffByShopQuery, Result<IReadOnlyList<StaffDto>>>
{
    private readonly IStaffRepository _staffRepository;

    public GetStaffByShopHandler(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public async Task<Result<IReadOnlyList<StaffDto>>> Handle(GetStaffByShopQuery request, CancellationToken cancellationToken)
    {
        var staffList = await _staffRepository.GetByShopIdAsync(request.ShopId, cancellationToken);

        var dtoList = staffList.Select(s => new StaffDto(
            s.Id,
            s.FirstName,
            s.LastName,
            s.Role,
            new ContactInfoDto(s.Contact.Phone, s.Contact.Email),
            s.EmploymentStatus,
            s.HireDate,
            s.ShopAssignments.Select(sa => new StaffShopAssignmentDto(
                sa.Id,
                sa.StaffId,
                sa.ShopId,
                sa.AssignedDate,
                sa.UnassignedDate,
                sa.IsPrimary
            )).ToList(),
            s.CreatedAt,
            s.UpdatedAt,
            s.DeletedAt)).ToList();

        return Result.Ok<IReadOnlyList<StaffDto>>(dtoList);
    }
}
