using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Application.Common.Dtos;

public record OrderDto(
    ObjectId Id,
    ObjectId ShopId,
    ObjectId StaffId,
    CustomerInfoDto CustomerInfo,
    IReadOnlyList<OrderItemDto> Items,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    MoneyDto TotalAmount,
    DateTime OrderTime,
    DateTime? CompletedTime,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record OrderItemDto(
    ObjectId MenuItemId,
    string Name,
    string Description,
    MoneyDto UnitPrice,
    int Quantity,
    string SpecialInstructions,
    MoneyDto TotalPrice
);

public record CustomerInfoDto(
    string Name,
    string Contact,
    CustomerType Type
);

public enum CustomerType
{
    DineIn = 1,
    Takeaway = 2,
    Online = 3
}

public record OrderSummaryDto(
    ObjectId Id,
    DateTime OrderTime,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    MoneyDto TotalAmount,
    string CustomerName,
    CustomerType CustomerType
);