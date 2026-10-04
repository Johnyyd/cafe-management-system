using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Menu;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Menu.Queries;

public record GetMenuItemByIdQuery(ObjectId Id) : IRequest<Result<MenuItemDto>>;

public record GetMenuItemsQuery(
    int Page = 1,
    int PageSize = 20,
    ObjectId? ShopId = null,
    string? Category = null,
    MenuItemStatus? Status = null
) : IRequest<Result<PagedResult<MenuItemDto>>>;

public record GetCategoriesQuery : IRequest<Result<IReadOnlyList<string>>>;

public record GetMenuItemsByShopQuery(ObjectId ShopId) : IRequest<Result<IReadOnlyList<MenuItemDto>>>;
