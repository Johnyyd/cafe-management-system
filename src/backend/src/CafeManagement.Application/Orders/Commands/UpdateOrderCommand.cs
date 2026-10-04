using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Orders.Commands;

public record UpdateOrderCommand(
    ObjectId Id,
    CustomerInfoDto? CustomerInfo
) : IRequest<Result>;

public record CancelOrderCommand(ObjectId Id) : IRequest<Result>;

public record ProcessPaymentCommand(
    ObjectId Id,
    PaymentMethod PaymentMethod,
    decimal Amount
) : IRequest<Result>;

public record UpdateOrderStatusCommand(
    ObjectId Id,
    OrderStatus Status
) : IRequest<Result>;

public record AddOrderItemCommand(
    ObjectId OrderId,
    ObjectId MenuItemId,
    int Quantity,
    string? SpecialInstructions
) : IRequest<Result>;

public record UpdateOrderItemCommand(
    ObjectId OrderId,
    ObjectId OrderItemId,
    int Quantity,
    string? SpecialInstructions
) : IRequest<Result>;

public record RemoveOrderItemCommand(
    ObjectId OrderId,
    ObjectId OrderItemId
) : IRequest<Result>;

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    Mobile = 3,
    Other = 4
}
