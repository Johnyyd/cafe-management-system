using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Application.Common.Dtos;

public record ReviewItemDto(
    ObjectId MenuItemId,
    string MenuItemName,
    int Rating,
    string? Comment
);

public record CreateReviewRequest(
    ObjectId ShopId,
    string ShopName,
    ObjectId? OrderId,
    string CustomerName,
    string CustomerPhone,
    int ShopRating,
    string ShopComment,
    IReadOnlyList<ReviewItemDto> ItemReviews,
    IReadOnlyList<string> Tags,
    DateTime? CreatedAt
);

public record ReviewDto(
    ObjectId Id,
    ObjectId ShopId,
    string ShopName,
    ObjectId? OrderId,
    string CustomerName,
    string CustomerPhone,
    int ShopRating,
    string ShopComment,
    IReadOnlyList<ReviewItemDto> ItemReviews,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt
);

public record OrderContextDto(
    ObjectId OrderId,
    string OrderNumber,
    ObjectId ShopId,
    string ShopName,
    string CustomerName,
    IReadOnlyList<OrderContextItemDto> Items
);

public record OrderContextItemDto(
    ObjectId MenuItemId,
    string MenuItemName,
    int Quantity
);