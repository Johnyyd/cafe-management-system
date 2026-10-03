using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Menu.Commands;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Menu;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Menu.Handlers;

public class CreateMenuItemHandler : IRequestHandler<CreateMenuItemCommand, Result<ObjectId>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateMenuItemHandler> _logger;

    public CreateMenuItemHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateMenuItemHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ObjectId>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating menu item with name: {Name} for shop: {ShopId}", request.Name, request.ShopId);

        try
        {
            // Check if menu item with same name already exists in this shop
            var existingItem = await _menuItemRepository.GetByShopIdAndNameAsync(request.ShopId, request.Name, cancellationToken);
            if (existingItem != null)
            {
                return Result.Fail(DomainErrors.General.AlreadyExists("MenuItem", "Name", request.Name));
            }

            // Create domain object
            var price = new Money(request.Price.Amount, request.Price.Currency);
            var availability = new Availability(
                request.Availability.StartTime,
                request.Availability.EndTime,
                request.Availability.DaysOfWeek);

            var menuItemResult = Domain.Menu.MenuItem.Create(
                request.ShopId,
                request.Category,
                request.Name,
                request.Description,
                price,
                request.Ingredients,
                request.Allergens,
                availability);

            if (menuItemResult.IsFailed)
            {
                return Result.Fail(menuItemResult.Errors);
            }

            var menuItem = menuItemResult.Value;

            // Save to repository
            await _menuItemRepository.AddAsync(menuItem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Menu item created successfully with Id: {MenuItemId}", menuItem.Id);

            return Result.Ok(menuItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu item");
            return Result.Fail(DomainErrors.General.InvalidOperation(ex.Message));
        }
    }
}