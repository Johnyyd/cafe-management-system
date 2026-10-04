using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Staff.Queries;

public record GetStaffByIdQuery(ObjectId Id) : IRequest<Result<StaffDto>>;

public record GetStaffQuery(
    int Page = 1,
    int PageSize = 20,
    ObjectId? ShopId = null,
    StaffRole? Role = null,
    EmploymentStatus? Status = null
) : IRequest<Result<PagedResult<StaffDto>>>;

public record GetStaffByShopQuery(ObjectId ShopId) : IRequest<Result<IReadOnlyList<StaffDto>>>;
