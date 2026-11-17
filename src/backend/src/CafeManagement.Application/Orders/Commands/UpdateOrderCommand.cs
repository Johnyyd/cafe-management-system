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

public record DeleteOrderCommand(ObjectId Id) : IRequest<Result>;

public record ProcessPaymentCommand(
    ObjectId Id
) : IRequest<Result>;

public record PayAndCompleteCommand(
    ObjectId Id
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
