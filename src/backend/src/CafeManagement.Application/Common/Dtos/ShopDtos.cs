using CafeManagement.Domain.Shops;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Application.Common.Dtos;

public record ShopDto(
    ObjectId Id,
    string Name,
    AddressDto Address,
    ContactInfoDto Contact,
    IReadOnlyList<OperatingHoursDto> OperatingHours,
    ShopStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record AddressDto(
    string Street,
    string City,
    string District,
    string ZipCode
);

public record ContactInfoDto(
    string Phone,
    string Email
);

public record OperatingHoursDto(
    int DayOfWeek,
    TimeSpan OpenTime,
    TimeSpan CloseTime
);

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserDto User
);

public record UserDto(
    ObjectId Id,
    string Email,
    string FullName,
    ObjectId? ShopId,
    IEnumerable<string> Roles
);
