using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Staff.Commands;

public record UpdateStaffCommand(
    ObjectId Id,
    string FirstName,
    string LastName,
    ContactInfoDto Contact
) : IRequest<Result>;

public record UpdateStaffRoleCommand(
    ObjectId Id,
    StaffRole Role
) : IRequest<Result>;

public record UpdateStaffEmploymentStatusCommand(
    ObjectId Id,
    EmploymentStatus Status
) : IRequest<Result>;

public record AssignStaffToShopCommand(
    ObjectId Id,
    ObjectId ShopId,
    bool IsPrimary
) : IRequest<Result>;

public record UnassignStaffFromShopCommand(
    ObjectId Id,
    ObjectId ShopId
) : IRequest<Result>;

public record SetStaffPrimaryShopCommand(
    ObjectId Id,
    ObjectId ShopId
) : IRequest<Result>;

public record DeactivateStaffCommand(ObjectId Id) : IRequest<Result>;
