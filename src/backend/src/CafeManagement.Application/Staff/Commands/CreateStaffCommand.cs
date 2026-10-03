using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Staff.Commands;

public record CreateStaffCommand(
    string FirstName,
    string LastName,
    StaffRole Role,
    ContactInfoDto Contact,
    DateTime HireDate,
    ObjectId ShopId
) : IRequest<Result<ObjectId>>;