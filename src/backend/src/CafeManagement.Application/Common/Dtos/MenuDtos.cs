using CafeManagement.Domain.Menu;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Application.Common.Dtos;

public record MenuItemDto(
    ObjectId Id,
    ObjectId ShopId,
    string Category,
    string Name,
    string Description,
    MoneyDto Price,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Allergens,
    AvailabilityDto Availability,
    MenuItemStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record MoneyDto(
    decimal Amount,
    string Currency
);

public record AvailabilityDto(
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    IReadOnlyList<int> DaysOfWeek
);