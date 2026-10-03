using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Application.Shops.Queries;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Shops.Handlers;

public class UpdateShopHandler : IRequestHandler<UpdateShopCommand, FluentResults.Result>
{
    private readonly IShopRepository _shopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateShopHandler> _logger;

    public UpdateShopHandler(IShopRepository shopRepository, IUnitOfWork unitOfWork, ILogger<UpdateShopHandler> logger)
    {
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateShopCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shop == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Shop", request.Id));

        var address = new Address(request.Address.Street, request.Address.City, request.Address.District, request.Address.ZipCode);
        var contact = new ContactInfo(request.Contact.Phone, request.Contact.Email);

        var result = shop.Update(request.Name, address, contact);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _shopRepository.UpdateAsync(shop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop updated: {ShopId}", shop.Id);
        return FluentResults.Result.Ok();
    }
}

public class DeactivateShopHandler : IRequestHandler<DeactivateShopCommand, FluentResults.Result>
{
    private readonly IShopRepository _shopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateShopHandler> _logger;

    public DeactivateShopHandler(IShopRepository shopRepository, IUnitOfWork unitOfWork, ILogger<DeactivateShopHandler> logger)
    {
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(DeactivateShopCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shop == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Shop", request.Id));

        var result = shop.Deactivate();
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _shopRepository.UpdateAsync(shop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop deactivated: {ShopId}", shop.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateOperatingHoursHandler : IRequestHandler<UpdateOperatingHoursCommand, FluentResults.Result>
{
    private readonly IShopRepository _shopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOperatingHoursHandler> _logger;

    public UpdateOperatingHoursHandler(IShopRepository shopRepository, IUnitOfWork unitOfWork, ILogger<UpdateOperatingHoursHandler> logger)
    {
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateOperatingHoursCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        if (shop == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Shop", request.ShopId));

        var operatingHours = request.OperatingHours.Select(h => new OperatingHours(h.DayOfWeek, h.OpenTime, h.CloseTime)).ToList();

        var result = shop.UpdateOperatingHours(operatingHours);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _shopRepository.UpdateAsync(shop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Operating hours updated for shop: {ShopId}", shop.Id);
        return FluentResults.Result.Ok();
    }
}

public class GetShopByIdHandler : IRequestHandler<GetShopByIdQuery, FluentResults.Result<ShopDto>>
{
    private readonly IShopRepository _shopRepository;

    public GetShopByIdHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<Result<ShopDto>> Handle(GetShopByIdQuery request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shop == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Shop", request.Id));

        var dto = new ShopDto(
            shop.Id,
            shop.Name,
            new AddressDto(shop.Address.Street, shop.Address.City, shop.Address.District, shop.Address.ZipCode),
            new ContactInfoDto(shop.Contact.Phone, shop.Contact.Email),
            shop.OperatingHours.Select(h => new OperatingHoursDto(h.DayOfWeek, h.OpenTime, h.CloseTime)).ToList(),
            shop.Status,
            shop.CreatedAt,
            shop.UpdatedAt,
            shop.DeletedAt);

        return FluentResults.Result.Ok(dto);
    }
}

public class GetShopsHandler : IRequestHandler<GetShopsQuery, FluentResults.Result<PagedResult<ShopDto>>>
{
    private readonly IShopRepository _shopRepository;

    public GetShopsHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<Result<PagedResult<ShopDto>>> Handle(GetShopsQuery request, CancellationToken cancellationToken)
    {
        var shops = await _shopRepository.ListAsync(cancellationToken);

        // Simple filtering (in production, do this at DB level)
        if (!string.IsNullOrWhiteSpace(request.Name))
            shops = shops.Where(s => s.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (request.Status.HasValue)
            shops = shops.Where(s => s.Status == request.Status.Value).ToList();

        var totalCount = shops.Count;
        var pagedShops = shops
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShopDto(
                s.Id,
                s.Name,
                new AddressDto(s.Address.Street, s.Address.City, s.Address.District, s.Address.ZipCode),
                new ContactInfoDto(s.Contact.Phone, s.Contact.Email),
                s.OperatingHours.Select(h => new OperatingHoursDto(h.DayOfWeek, h.OpenTime, h.CloseTime)).ToList(),
                s.Status,
                s.CreatedAt,
                s.UpdatedAt,
                s.DeletedAt))
            .ToList();

        var result = new PagedResult<ShopDto>(pagedShops, totalCount, request.Page, request.PageSize);
        return FluentResults.Result.Ok(result);
    }
}

public class GetShopOperatingHoursHandler : IRequestHandler<GetShopOperatingHoursQuery, FluentResults.Result<IReadOnlyList<OperatingHoursDto>>>
{
    private readonly IShopRepository _shopRepository;

    public GetShopOperatingHoursHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<Result<IReadOnlyList<OperatingHoursDto>>> Handle(GetShopOperatingHoursQuery request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        if (shop == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Shop", request.ShopId));

        var hours = shop.OperatingHours.Select(h => new OperatingHoursDto(h.DayOfWeek, h.OpenTime, h.CloseTime)).ToList();
        return FluentResults.Result.Ok((IReadOnlyList<OperatingHoursDto>)hours);
    }
}