using CafeManagement.Domain.Staff;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Application.Common.Dtos;

public record StaffDto(
    ObjectId Id,
    string FirstName,
    string LastName,
    StaffRole Role,
    ContactInfoDto Contact,
    EmploymentStatus EmploymentStatus,
    DateTime HireDate,
    IReadOnlyList<StaffShopAssignmentDto> ShopAssignments,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record StaffShopAssignmentDto(
    ObjectId Id,
    ObjectId StaffId,
    ObjectId ShopId,
    DateTime AssignedDate,
    DateTime? UnassignedDate,
    bool IsPrimary
);

public record StaffSummaryDto(
    ObjectId Id,
    string FullName,
    StaffRole Role,
    EmploymentStatus EmploymentStatus
);