using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Menu;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Menu.Commands;

public record CreateMenuItemCommand(
    ObjectId ShopId,
    string Category,
    string Name,
    string Description,
    MoneyDto Price,
    IEnumerable<string> Ingredients,
    IEnumerable<string> Allergens,
    AvailabilityDto Availability
) : IRequest<Result<ObjectId>>;