using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.ShiftSwaps.Queries;

public record GetShiftSwapByIdQuery(ObjectId Id) : IRequest<FluentResults.Result<ShiftSwapDto>>;

public record GetShiftSwapsQuery(
    ObjectId? RequestingStaffId = null,
    ObjectId? RequestedStaffId = null,
    ObjectId? ShopId = null,
    ShiftSwapStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<ShiftSwapDto>>>;

public record GetShiftSwapsByStaffQuery(
    ObjectId StaffId,
    ShiftSwapStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FluentResults.Result<PagedResult<ShiftSwapDto>>>;

public record GetPendingShiftSwapsQuery(
    ObjectId StaffId
) : IRequest<FluentResults.Result<IReadOnlyList<ShiftSwapDto>>>;

public record GetPendingShiftSwapsForApprovalQuery(
    ObjectId ShopId
) : IRequest<FluentResults.Result<IReadOnlyList<ShiftSwapDto>>>;