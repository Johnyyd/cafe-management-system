using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Orders.Queries;

public record GetOrderByIdQuery(ObjectId Id) : IRequest<Result<OrderDto>>;

public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    ObjectId? ShopId = null,
    OrderStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<PagedResult<OrderDto>>>;

public record GetOrderReceiptQuery(ObjectId Id) : IRequest<Result<OrderDto>>;

public record GetOrdersByShopQuery(ObjectId ShopId) : IRequest<Result<IReadOnlyList<OrderDto>>>;
