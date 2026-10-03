using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Shops.Commands;

public record UpdateShopCommand(
    ObjectId Id,
    string Name,
    AddressDto Address,
    ContactInfoDto Contact
) : IRequest<Result>;

public record DeactivateShopCommand(ObjectId Id) : IRequest<Result>;

public record UpdateOperatingHoursCommand(
    ObjectId ShopId,
    IEnumerable<OperatingHoursDto> OperatingHours
) : IRequest<Result>;