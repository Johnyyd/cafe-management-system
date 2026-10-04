using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Menu.Commands;
using CafeManagement.Application.Menu.Queries;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Menu;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Menu.Handlers;

public class UpdateMenuItemHandler : IRequestHandler<UpdateMenuItemCommand, FluentResults.Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateMenuItemHandler> _logger;

    public UpdateMenuItemHandler(IMenuItemRepository menuItemRepository, IUnitOfWork unitOfWork, ILogger<UpdateMenuItemHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (menuItem == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("MenuItem", request.Id));

        var result = menuItem.Update(
            request.Category ?? menuItem.Category,
            request.Name ?? menuItem.Name,
            request.Description ?? menuItem.Description,
            request.Price != null ? new Money(request.Price.Amount, request.Price.Currency) : menuItem.Price,
            request.Ingredients ?? menuItem.Ingredients,
            request.Allergens ?? menuItem.Allergens,
            request.Availability != null ? new Availability(
                request.Availability.StartTime,
                request.Availability.EndTime,
                request.Availability.DaysOfWeek) : menuItem.Availability);

        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _menuItemRepository.UpdateAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Menu item updated: {MenuItemId}", menuItem.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateMenuItemAvailabilityHandler : IRequestHandler<UpdateMenuItemAvailabilityCommand, FluentResults.Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateMenuItemAvailabilityHandler> _logger;

    public UpdateMenuItemAvailabilityHandler(IMenuItemRepository menuItemRepository, IUnitOfWork unitOfWork, ILogger<UpdateMenuItemAvailabilityHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateMenuItemAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (menuItem == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("MenuItem", request.Id));

        var availability = new Availability(
            request.Availability.StartTime,
            request.Availability.EndTime,
            request.Availability.DaysOfWeek);

        var result = menuItem.UpdateAvailability(availability);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _menuItemRepository.UpdateAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Menu item availability updated: {MenuItemId}", menuItem.Id);
        return FluentResults.Result.Ok();
    }
}

public class DeactivateMenuItemHandler : IRequestHandler<DeactivateMenuItemCommand, FluentResults.Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateMenuItemHandler> _logger;

    public DeactivateMenuItemHandler(IMenuItemRepository menuItemRepository, IUnitOfWork unitOfWork, ILogger<DeactivateMenuItemHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(DeactivateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (menuItem == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("MenuItem", request.Id));

        var result = menuItem.Deactivate();
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _menuItemRepository.UpdateAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Menu item deactivated: {MenuItemId}", menuItem.Id);
        return FluentResults.Result.Ok();
    }
}

// Query handlers
public class GetMenuItemByIdHandler : IRequestHandler<GetMenuItemByIdQuery, FluentResults.Result<MenuItemDto>>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuItemByIdHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<Result<MenuItemDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (menuItem == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("MenuItem", request.Id));

        var dto = new MenuItemDto(
            menuItem.Id,
            menuItem.ShopId,
            menuItem.Category,
            menuItem.Name,
            menuItem.Description,
            new MoneyDto(menuItem.Price.Amount, menuItem.Price.Currency),
            menuItem.Ingredients,
            menuItem.Allergens,
            new AvailabilityDto(menuItem.Availability.StartTime, menuItem.Availability.EndTime, menuItem.Availability.DaysOfWeek),
            menuItem.Status,
            menuItem.CreatedAt,
            menuItem.UpdatedAt,
            menuItem.DeletedAt);

        return FluentResults.Result.Ok(dto);
    }
}

public class GetMenuItemsHandler : IRequestHandler<GetMenuItemsQuery, FluentResults.Result<PagedResult<MenuItemDto>>>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuItemsHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<Result<PagedResult<MenuItemDto>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _menuItemRepository.ListAsync(cancellationToken);

        if (request.ShopId.HasValue)
            items = items.Where(i => i.ShopId == request.ShopId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(request.Category))
            items = items.Where(i => i.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase)).ToList();

        if (request.Status.HasValue)
            items = items.Where(i => i.Status == request.Status.Value).ToList();

        var totalCount = items.Count;
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new MenuItemDto(
                i.Id,
                i.ShopId,
                i.Category,
                i.Name,
                i.Description,
                new MoneyDto(i.Price.Amount, i.Price.Currency),
                i.Ingredients,
                i.Allergens,
                new AvailabilityDto(i.Availability.StartTime, i.Availability.EndTime, i.Availability.DaysOfWeek),
                i.Status,
                i.CreatedAt,
                i.UpdatedAt,
                i.DeletedAt))
            .ToList();

        var result = new PagedResult<MenuItemDto>(pagedItems, totalCount, request.Page, request.PageSize);
        return FluentResults.Result.Ok(result);
    }
}

public class GetCategoriesHandler : IRequestHandler<CafeManagement.Application.Menu.Queries.GetCategoriesQuery, FluentResults.Result<IReadOnlyList<string>>>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetCategoriesHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(CafeManagement.Application.Menu.Queries.GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _menuItemRepository.GetCategoriesAsync(ObjectId.Empty, cancellationToken);
        return FluentResults.Result.Ok(categories);
    }
}
