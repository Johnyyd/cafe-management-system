using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Shifts.Commands;
using CafeManagement.Application.Shifts.Queries;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Shifts.Handlers;

public class CreateShiftHandler : IRequestHandler<CreateShiftCommand, Result<ObjectId>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateShiftHandler> _logger;

    public CreateShiftHandler(
        IShiftRepository shiftRepository,
        IStaffRepository staffRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateShiftHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ObjectId>> Handle(CreateShiftCommand request, CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(request.StaffId, cancellationToken);
        if (staff == null)
            return Result.Fail(DomainErrors.General.NotFound("Staff", request.StaffId));

        var shiftResult = Shift.Schedule(
            request.StaffId,
            request.ShopId,
            request.StartTime,
            request.EndTime,
            request.Notes);

        if (shiftResult.IsFailed)
            return Result.Fail(shiftResult.Errors);

        var shift = shiftResult.Value;
        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shift scheduled: {ShiftId} for Staff {StaffId} at Shop {ShopId}", shift.Id, request.StaffId, request.ShopId);
        return Result.Ok(shift.Id);
    }
}

public class GetShiftsHandler : IRequestHandler<GetShiftsQuery, Result<PagedResult<ShiftDto>>>
{
    private readonly IShiftRepository _shiftRepository;

    public GetShiftsHandler(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task<Result<PagedResult<ShiftDto>>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Shift> shifts;

        if (request.ShopId.HasValue && request.ShopId.Value != ObjectId.Empty)
        {
            shifts = await _shiftRepository.GetByShopIdAsync(
                request.ShopId.Value,
                request.StartDate,
                request.EndDate,
                request.Status,
                cancellationToken);
        }
        else if (request.StaffId.HasValue && request.StaffId.Value != ObjectId.Empty)
        {
            shifts = await _shiftRepository.GetByStaffIdAsync(
                request.StaffId.Value,
                request.StartDate,
                request.EndDate,
                request.Status,
                cancellationToken);
        }
        else
        {
            shifts = await _shiftRepository.ListAsync(cancellationToken);
            if (request.StartDate.HasValue)
                shifts = shifts.Where(s => s.EndTime >= request.StartDate.Value).ToList();
            if (request.EndDate.HasValue)
                shifts = shifts.Where(s => s.StartTime <= request.EndDate.Value).ToList();
            if (request.Status.HasValue)
                shifts = shifts.Where(s => s.Status == request.Status.Value).ToList();
        }

        var totalCount = shifts.Count;
        var pagedShifts = shifts
            .OrderByDescending(s => s.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShiftDto(
                s.Id,
                s.StaffId,
                s.ShopId,
                s.StartTime,
                s.EndTime,
                s.Status,
                s.Notes,
                s.CoveredByStaffId,
                s.CoveredAt,
                s.CoveredBy,
                s.SwappedAt,
                s.SwappedWithStaffId,
                s.SwappedWithShiftId,
                s.CreatedAt,
                s.UpdatedAt,
                s.DeletedAt))
            .ToList();

        var pagedResult = new PagedResult<ShiftDto>(
            pagedShifts,
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Ok(pagedResult);
    }
}

public class StartShiftHandler : IRequestHandler<StartShiftCommand, Result>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartShiftHandler(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(StartShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shift == null)
            return Result.Fail(DomainErrors.General.NotFound("Shift", request.Id));

        var result = shift.Start();
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _shiftRepository.UpdateAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

public class CompleteShiftHandler : IRequestHandler<CompleteShiftCommand, Result>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteShiftHandler(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CompleteShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shift == null)
            return Result.Fail(DomainErrors.General.NotFound("Shift", request.Id));

        var result = shift.Complete();
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _shiftRepository.UpdateAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

public class CancelShiftHandler : IRequestHandler<CancelShiftCommand, Result>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelShiftHandler(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shift == null)
            return Result.Fail(DomainErrors.General.NotFound("Shift", request.Id));

        var result = shift.Cancel(request.Reason);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _shiftRepository.UpdateAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

public class DeleteShiftHandler : IRequestHandler<DeleteShiftCommand, Result>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteShiftHandler(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shift == null)
            return Result.Fail(DomainErrors.General.NotFound("Shift", request.Id));

        shift.MarkAsDeleted();
        await _shiftRepository.UpdateAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

public class UpdateShiftHandler : IRequestHandler<UpdateShiftCommand, Result>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShiftHandler(IShiftRepository shiftRepository, IStaffRepository staffRepository, IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shift == null)
            return Result.Fail(DomainErrors.General.NotFound("Shift", request.Id));

        var staff = await _staffRepository.GetByIdAsync(request.StaffId, cancellationToken);
        if (staff == null)
            return Result.Fail(DomainErrors.General.NotFound("Staff", request.StaffId));

        var result = shift.UpdateDetails(request.StaffId, request.StartTime, request.EndTime, request.Notes);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _shiftRepository.UpdateAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
