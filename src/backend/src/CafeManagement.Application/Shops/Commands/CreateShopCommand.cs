using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Shops.Commands;

public record CreateShopCommand(
    string Name,
    AddressDto Address,
    ContactInfoDto Contact,
    IEnumerable<OperatingHoursDto> OperatingHours
) : IRequest<Result<ObjectId>>;