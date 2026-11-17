using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Runtime.InteropServices;

namespace CafeManagement.Application.Orders.Handlers;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<ObjectId>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ILogger<CreateOrderHandler> _logger;

    // Vietnam timezone (UTC+7) - all shops are in Vietnam
    // Windows uses "SE Asia Standard Time", Linux uses "Asia/Ho_Chi_Minh"
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh");

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IMenuItemRepository menuItemRepository,
        ILogger<CreateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
        _logger = logger;
    }

    public async Task<Result<ObjectId>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for shop: {ShopId} by staff: {StaffId}", request.ShopId, request.StaffId);

        // Convert DTOs to domain objects
        var customerInfo = new CustomerInfo(
            request.CustomerInfo.Name,
            request.CustomerInfo.Contact,
            (Domain.Orders.CustomerType)request.CustomerInfo.Type);

        // Get current time in Vietnam timezone for availability checks
        var nowInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        var orderItems = new List<OrderItem>();
        foreach (var itemDto in request.Items)
        {
            // Validate menu item exists and is available
            var menuItem = await _menuItemRepository.GetByIdAsync(itemDto.MenuItemId, cancellationToken);
            if (menuItem == null)
            {
                return Result.Fail(DomainErrors.General.NotFound("MenuItem", itemDto.MenuItemId));
            }

            if (!menuItem.IsAvailableAt(nowInVietnam))
            {
                return Result.Fail(DomainErrors.Business.InvalidOperation($"Menu item '{menuItem.Name}' is not available at this time"));
            }

            if (menuItem.ShopId != request.ShopId)
            {
                return Result.Fail(DomainErrors.Business.InvalidOperation($"Menu item '{menuItem.Name}' does not belong to this shop"));
            }

            // Prices and item details always come from the menu, never from the client payload.
            var unitPrice = new Money(menuItem.Price.Amount, menuItem.Price.Currency);
            var orderItem = new OrderItem(
                menuItem.Id,
                menuItem.Name,
                menuItem.Description,
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
            orderItems,
            request.OrderTime ?? DateTime.UtcNow);

        if (orderResult.IsFailed)
        {
            return Result.Fail(orderResult.Errors);
        }

        var order = orderResult.Value;

        // Save to repository (single-document atomic operation)
        await _orderRepository.AddAsync(order, cancellationToken);

        _logger.LogInformation("Order created successfully with Id: {OrderId}", order.Id);

        return Result.Ok(order.Id);
    }
}