using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Shops.Queries;

public record GetShopByIdQuery(ObjectId Id) : IRequest<Result<ShopDto>>;

public record GetShopsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Name = null,
    ShopStatus? Status = null
) : IRequest<Result<PagedResult<ShopDto>>>;

public record GetShopOperatingHoursQuery(ObjectId ShopId) : IRequest<Result<IReadOnlyList<OperatingHoursDto>>>;