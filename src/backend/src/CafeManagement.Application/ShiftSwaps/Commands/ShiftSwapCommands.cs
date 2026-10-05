using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.ShiftSwaps.Commands;

public record RequestShiftSwapCommand(
    ObjectId RequestingStaffId,
    ObjectId RequestedStaffId,
    ObjectId RequestingShiftId,
    ObjectId RequestedShiftId,
    ObjectId ShopId,
    string? Reason
) : IRequest<Result<ObjectId>>;

public record AcceptShiftSwapCommand(
    ObjectId Id
) : IRequest<Result>;

public record RejectShiftSwapCommand(
    ObjectId Id,
    string RejectionReason
) : IRequest<Result>;

public record ApproveShiftSwapCommand(
    ObjectId Id,
    string? ApprovalNotes
) : IRequest<Result>;

public record CancelShiftSwapCommand(
    ObjectId Id
) : IRequest<Result>;

public record UpdateShiftSwapReasonCommand(
    ObjectId Id,
    string? Reason
) : IRequest<Result>;

public record DeleteShiftSwapCommand(ObjectId Id) : IRequest<Result>;