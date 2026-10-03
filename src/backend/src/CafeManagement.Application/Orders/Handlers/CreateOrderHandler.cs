using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Orders.Handlers;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<ObjectId>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ObjectId>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for shop: {ShopId} by staff: {StaffId}", request.ShopId, request.StaffId);

        try
        {
            // Convert DTOs to domain objects
            var customerInfo = new CustomerInfo(
                request.CustomerInfo.Name,
                request.CustomerInfo.Contact,
                (Domain.Orders.CustomerType)request.CustomerInfo.Type);

            var orderItems = new List<OrderItem>();
            foreach (var itemDto in request.Items)
            {
                // Validate menu item exists and is available
                var menuItem = await _menuItemRepository.GetByIdAsync(itemDto.MenuItemId, cancellationToken);
                if (menuItem == null)
                {
                    return Result.Fail(DomainErrors.General.NotFound("MenuItem", itemDto.MenuItemId));
                }

                if (!menuItem.IsAvailableAt(DateTime.UtcNow))
                {
                    return Result.Fail(DomainErrors.Business.InvalidOperation($"Menu item '{menuItem.Name}' is not available at this time"));
                }

                var unitPrice = new Money(itemDto.UnitPrice.Amount, itemDto.UnitPrice.Currency);
                var orderItem = new OrderItem(
                    itemDto.MenuItemId,
                    itemDto.Name,
                    itemDto.Description,
                    unitPrice,
                    itemDto.Quantity,
                    itemDto.SpecialInstructions);

                orderItems.Add(orderItem);
            }

            // Create domain object
            var orderResult = Order.Create(
                request.ShopId,
                request.StaffId,
                customerInfo,
                orderItems);

            if (orderResult.IsFailed)
            {
                return Result.Fail(orderResult.Errors);
            }

            var order = orderResult.Value;

            // Save to repository
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order created successfully with Id: {OrderId}", order.Id);

            return Result.Ok(order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return Result.Fail(DomainErrors.General.InvalidOperation(ex.Message));
        }
    }
}