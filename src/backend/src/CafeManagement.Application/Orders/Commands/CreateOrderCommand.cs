using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Orders.Commands;

public record CreateOrderCommand(
    ObjectId ShopId,
    ObjectId StaffId,
    CustomerInfoDto CustomerInfo,
    IEnumerable<OrderItemDto> Items
) : IRequest<Result<ObjectId>>;