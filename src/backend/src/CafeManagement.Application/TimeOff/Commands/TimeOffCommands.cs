using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.TimeOff.Commands;

public record SubmitTimeOffRequestCommand(
    ObjectId StaffId,
    ObjectId ShopId,
    TimeOffType Type,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
) : IRequest<Result<ObjectId>>;

public record ApproveTimeOffRequestCommand(
    ObjectId Id,
    string? ApprovalNotes
) : IRequest<Result>;

public record RejectTimeOffRequestCommand(
    ObjectId Id,
    string RejectionReason
) : IRequest<Result>;

public record CancelTimeOffRequestCommand(
    ObjectId Id
) : IRequest<Result>;

public record UpdateTimeOffRequestReasonCommand(
    ObjectId Id,
    string? Reason
) : IRequest<Result>;

public record UpdateTimeOffRequestDatesCommand(
    ObjectId Id,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<Result>;

public record DeleteTimeOffRequestCommand(ObjectId Id) : IRequest<Result>;