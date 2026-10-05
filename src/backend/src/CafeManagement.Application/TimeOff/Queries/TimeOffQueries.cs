using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.TimeOff.Queries;

public record GetTimeOffRequestByIdQuery(ObjectId Id) : IRequest<FluentResults.Result<TimeOffRequestDto>>;

public record GetTimeOffRequestsQuery(
    ObjectId? StaffId = null,
    ObjectId? ShopId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    TimeOffStatus? Status = null,
    TimeOffType? Type = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<TimeOffRequestDto>>>;

public record GetTimeOffRequestsByStaffQuery(
    ObjectId StaffId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    TimeOffStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<TimeOffRequestDto>>>;

public record GetTimeOffRequestsByShopQuery(
    ObjectId ShopId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    TimeOffStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<TimeOffRequestDto>>>;

public record GetPendingTimeOffRequestsQuery(
    ObjectId ShopId
) : IRequest<FluentResults.Result<IReadOnlyList<TimeOffRequestDto>>>;

public record GetActiveTimeOffQuery(
    ObjectId? StaffId = null,
    ObjectId? ShopId = null
) : IRequest<FluentResults.Result<IReadOnlyList<TimeOffRequestDto>>>;