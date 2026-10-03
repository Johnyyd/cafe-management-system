using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Shops.Handlers;

public class CreateShopHandler : IRequestHandler<CreateShopCommand, Result<ObjectId>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateShopHandler> _logger;

    public CreateShopHandler(
        IShopRepository shopRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateShopHandler> logger)
    {
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ObjectId>> Handle(CreateShopCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating shop with name: {Name}", request.Name);

        try
        {
            // Check if shop with same name already exists
            var existingShop = await _shopRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingShop != null)
            {
                return Result.Fail(DomainErrors.General.AlreadyExists("Shop", "Name", request.Name));
            }

            // Create domain object
            var address = new Address(
                request.Address.Street,
                request.Address.City,
                request.Address.District,
                request.Address.ZipCode);

            var contact = new ContactInfo(
                request.Contact.Phone,
                request.Contact.Email);

            var operatingHours = request.OperatingHours.Select(h => new OperatingHours(
                h.DayOfWeek,
                h.OpenTime,
                h.CloseTime)).ToList();

            var shopResult = Shop.Create(
                request.Name,
                address,
                contact,
                operatingHours);

            if (shopResult.IsFailed)
            {
                return Result.Fail(shopResult.Errors);
            }

            var shop = shopResult.Value;

            // Save to repository
            await _shopRepository.AddAsync(shop, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Shop created successfully with Id: {ShopId}", shop.Id);

            return Result.Ok(shop.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shop");
            return Result.Fail(DomainErrors.General.InvalidOperation(ex.Message));
        }
    }
}