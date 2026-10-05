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
