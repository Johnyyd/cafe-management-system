using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Shifts.Queries;

public record GetShiftByIdQuery(ObjectId Id) : IRequest<FluentResults.Result<ShiftDto>>;

public record GetShiftsQuery(
    ObjectId? StaffId = null,
    ObjectId? ShopId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ShiftStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<ShiftDto>>>;

public record GetShiftsByStaffQuery(
    ObjectId StaffId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ShiftStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<ShiftDto>>>;

public record GetShiftsByShopQuery(
    ObjectId ShopId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ShiftStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<ShiftDto>>>;

public record GetUpcomingShiftsQuery(
    ObjectId? StaffId = null,
    ObjectId? ShopId = null,
    int Days = 7
) : IRequest<FluentResults.Result<IReadOnlyList<ShiftDto>>>;

public record GetShiftsNeedingCoverageQuery(
    ObjectId ShopId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<FluentResults.Result<IReadOnlyList<ShiftDto>>>;