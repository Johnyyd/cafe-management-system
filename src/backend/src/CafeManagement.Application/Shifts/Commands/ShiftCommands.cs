using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Shifts.Commands;

public record CreateShiftCommand(
    ObjectId StaffId,
    ObjectId ShopId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
) : IRequest<Result<ObjectId>>;

public record StartShiftCommand(
    ObjectId Id
) : IRequest<Result>;

public record CompleteShiftCommand(
    ObjectId Id
) : IRequest<Result>;

public record CancelShiftCommand(
    ObjectId Id,
    string? Reason
) : IRequest<Result>;

public record CoverShiftCommand(
    ObjectId Id,
    ObjectId CoveredByStaffId
) : IRequest<Result>;

public record UncoverShiftCommand(
    ObjectId Id
) : IRequest<Result>;

public record SwapShiftCommand(
    ObjectId Id,
    ObjectId SwappedWithShiftId,
    ObjectId SwappedWithStaffId
) : IRequest<Result>;

public record UnswapShiftCommand(
    ObjectId Id
) : IRequest<Result>;

public record UpdateShiftNotesCommand(
    ObjectId Id,
    string? Notes
) : IRequest<Result>;

public record DeleteShiftCommand(ObjectId Id) : IRequest<Result>;