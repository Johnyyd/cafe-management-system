using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Orders.Queries;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using DomainOrders = CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Runtime.InteropServices;

namespace CafeManagement.Application.Orders.Handlers;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(IOrderRepository orderRepository, ILogger<UpdateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        if (request.CustomerInfo != null)
        {
            var customerInfo = new DomainOrders.CustomerInfo(
                request.CustomerInfo.Name,
                request.CustomerInfo.Contact,
                (DomainOrders.CustomerType)request.CustomerInfo.Type);

            var result = order.UpdateCustomerInfo(customerInfo);
            if (result.IsFailed)
                return FluentResults.Result.Fail(result.Errors);
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order updated: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CancelOrderHandler> _logger;

    public CancelOrderHandler(IOrderRepository orderRepository, ILogger<CancelOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        var result = order.Cancel();
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order cancelled: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<DeleteOrderHandler> _logger;

    public DeleteOrderHandler(IOrderRepository orderRepository, ILogger<DeleteOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        await _orderRepository.DeleteAsync(request.Id, cancellationToken);

        _logger.LogInformation("Order deleted: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(IOrderRepository orderRepository, ILogger<ProcessPaymentHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        // Use server-calculated total amount instead of client-supplied amount
        var amountPaid = order.TotalAmount;

        var result = order.ProcessCashPayment(amountPaid);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Cash payment recorded for order: {OrderId}, amount: {Amount}", order.Id, amountPaid);
        return FluentResults.Result.Ok();
    }
}

public class PayAndCompleteHandler : IRequestHandler<PayAndCompleteCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PayAndCompleteHandler> _logger;

    public PayAndCompleteHandler(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        ILogger<PayAndCompleteHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(PayAndCompleteCommand request, CancellationToken cancellationToken)
    {
        // Start a MongoDB transaction for atomic order completion + inventory adjustment
        await _unitOfWork.StartTransactionAsync(cancellationToken);

        try
        {
            var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
            if (order == null)
            {
                await _unitOfWork.AbortTransactionAsync(cancellationToken);
                return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));
            }

            // Use server-calculated total amount instead of client-supplied amount
            var amountPaid = order.TotalAmount;

            var result = order.PayAndComplete(amountPaid);
            if (result.IsFailed)
            {
                await _unitOfWork.AbortTransactionAsync(cancellationToken);
                return FluentResults.Result.Fail(result.Errors);
            }

            // Resolve every inventory item first so a missing ingredient fails before
            // anything is deducted (there may be no transaction on standalone MongoDB).
            // Menu items are not mapped to ingredients (no recipe/BOM), so items without
            // a same-named inventory record are skipped instead of blocking payment.
            var deductions = new List<(CafeManagement.Domain.Inventory.InventoryItem Item, DomainOrders.OrderItem OrderItem)>();
            foreach (var orderItem in order.Items)
            {
                var inventoryResult = await _inventoryService.GetByShopIdAndNameAsync(order.ShopId, orderItem.Name);
                if (inventoryResult.IsFailed)
                {
                    _logger.LogInformation("No inventory record for {ItemName} in shop {ShopId}; skipping stock deduction for order {OrderId}",
                        orderItem.Name, order.ShopId, order.Id);
                    continue;
                }

                deductions.Add((inventoryResult.Value, orderItem));
            }

            // Deduct inventory for each order item, rolling back what was already
            // deducted if a later item fails (best effort when no transaction is active).
            var applied = new List<(CafeManagement.Domain.Inventory.InventoryItem Item, DomainOrders.OrderItem OrderItem)>();
            foreach (var (inventoryItem, orderItem) in deductions)
            {
                var adjustResult = await _inventoryService.AdjustQuantityAsync(
                    inventoryItem.Id,
                    -orderItem.Quantity, // Negative to reduce inventory
                    $"Order {order.OrderNumber} completed",
                    order.StaffId);

                if (adjustResult.IsFailed)
                {
                    foreach (var (doneItem, doneOrderItem) in applied)
                    {
                        await _inventoryService.AdjustQuantityAsync(
                            doneItem.Id,
                            doneOrderItem.Quantity,
                            $"Rollback for order {order.OrderNumber}",
                            order.StaffId);
                    }

                    await _unitOfWork.AbortTransactionAsync(cancellationToken);
                    _logger.LogWarning("Failed to adjust inventory for item {ItemName} in order {OrderId}: {Errors}",
                        orderItem.Name, order.Id, adjustResult.Errors);
                    return FluentResults.Result.Fail($"Inventory adjustment failed for {orderItem.Name}: {adjustResult.Errors.FirstOrDefault()?.Message}");
                }

                applied.Add((inventoryItem, orderItem));
            }

            // All inventory adjustments succeeded, save the completed order
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // Commit the transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Cash payment completed for order: {OrderId}, amount: {Amount}", order.Id, amountPaid);
            return FluentResults.Result.Ok();
        }
        catch (Exception ex)
        {
            await _unitOfWork.AbortTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Order {OrderId} completion failed, transaction aborted", request.Id);
            return FluentResults.Result.Fail($"Order completion failed: {ex.Message}. Transaction has been rolled back.");
        }
    }
}

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<UpdateOrderStatusHandler> _logger;

    public UpdateOrderStatusHandler(IOrderRepository orderRepository, ILogger<UpdateOrderStatusHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        var result = order.ChangeStatus(request.Status);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order status updated: {OrderId} to {Status}", order.Id, request.Status);
        return FluentResults.Result.Ok();
    }
}

public class AddOrderItemHandler : IRequestHandler<AddOrderItemCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ILogger<AddOrderItemHandler> _logger;

    // Vietnam timezone (UTC+7) - all shops are in Vietnam
    // Windows uses "SE Asia Standard Time", Linux uses "Asia/Ho_Chi_Minh"
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh");

    public AddOrderItemHandler(
        IOrderRepository orderRepository,
        IMenuItemRepository menuItemRepository,
        ILogger<AddOrderItemHandler> logger)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.OrderId));

        var menuItem = await _menuItemRepository.GetByIdAsync(request.MenuItemId, cancellationToken);
        if (menuItem == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("MenuItem", request.MenuItemId));

        // Get current time in Vietnam timezone for availability checks
        var nowInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        if (!menuItem.IsAvailableAt(nowInVietnam))
            return FluentResults.Result.Fail(DomainErrors.Business.InvalidOperation($"Menu item '{menuItem.Name}' is not available at this time"));

        var unitPrice = new Money(menuItem.Price.Amount, menuItem.Price.Currency);
        var orderItem = new DomainOrders.OrderItem(
            request.MenuItemId,
            menuItem.Name,
            menuItem.Description,
            unitPrice,
            request.Quantity,
            request.SpecialInstructions ?? string.Empty);

        var result = order.AddItem(orderItem);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order item added to order: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<UpdateOrderItemHandler> _logger;

    public UpdateOrderItemHandler(IOrderRepository orderRepository, ILogger<UpdateOrderItemHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.OrderId));

        var result = order.UpdateItemQuantity(request.OrderItemId, request.Quantity);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        if (!string.IsNullOrWhiteSpace(request.SpecialInstructions))
        {
            result = order.UpdateItemSpecialInstructions(request.OrderItemId, request.SpecialInstructions);
            if (result.IsFailed)
                return FluentResults.Result.Fail(result.Errors);
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order item updated: {OrderItemId} in order: {OrderId}", request.OrderItemId, request.OrderId);
        return FluentResults.Result.Ok();
    }
}

public class RemoveOrderItemHandler : IRequestHandler<RemoveOrderItemCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<RemoveOrderItemHandler> _logger;

    public RemoveOrderItemHandler(IOrderRepository orderRepository, ILogger<RemoveOrderItemHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(RemoveOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.OrderId));

        var result = order.RemoveItem(request.OrderItemId);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order item removed: {OrderItemId} from order: {OrderId}", request.OrderItemId, request.OrderId);
        return FluentResults.Result.Ok();
    }
}

// Query handlers
public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, FluentResults.Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        var dto = new OrderDto(
            order.Id,
            order.ShopId,
            order.StaffId,
            order.OrderNumber,
            new CustomerInfoDto(order.CustomerInfo.Name, order.CustomerInfo.Contact, (CustomerType)order.CustomerInfo.Type),
            order.Items.Select(i => new OrderItemDto(
                i.MenuItemId,
                i.Name,
                i.Description,
                new MoneyDto(i.UnitPrice.Amount, i.UnitPrice.Currency),
                i.Quantity,
                i.SpecialInstructions ?? string.Empty,
                new MoneyDto(i.TotalPrice.Amount, i.TotalPrice.Currency))).ToList(),
            order.Status,
            order.PaymentStatus,
            new MoneyDto(order.TotalAmount.Amount, order.TotalAmount.Currency),
            order.OrderTime,
            order.CompletedTime,
            order.CreatedAt,
            order.UpdatedAt,
            order.DeletedAt);

        return FluentResults.Result.Ok(dto);
    }
}

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, FluentResults.Result<PagedResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.ListAsync(cancellationToken);

        if (request.ShopId.HasValue)
            orders = orders.Where(o => o.ShopId == request.ShopId.Value).ToList();

        if (request.Status.HasValue)
            orders = orders.Where(o => o.Status == request.Status.Value).ToList();

        if (request.FromDate.HasValue)
            orders = orders.Where(o => o.OrderTime >= request.FromDate.Value).ToList();

        if (request.ToDate.HasValue)
            orders = orders.Where(o => o.OrderTime <= request.ToDate.Value).ToList();

        orders = orders.OrderByDescending(o => o.OrderTime).ToList();

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        var totalCount = orders.Count;
        var pagedOrders = orders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.ShopId,
                o.StaffId,
                o.OrderNumber,
                new CustomerInfoDto(o.CustomerInfo.Name, o.CustomerInfo.Contact, (CustomerType)o.CustomerInfo.Type),
                o.Items.Select(i => new OrderItemDto(
                    i.MenuItemId,
                    i.Name,
                    i.Description,
                    new MoneyDto(i.UnitPrice.Amount, i.UnitPrice.Currency),
                    i.Quantity,
                    i.SpecialInstructions ?? string.Empty,
                    new MoneyDto(i.TotalPrice.Amount, i.TotalPrice.Currency))).ToList(),
                o.Status,
                o.PaymentStatus,
                new MoneyDto(o.TotalAmount.Amount, o.TotalAmount.Currency),
                o.OrderTime,
                o.CompletedTime,
                o.CreatedAt,
                o.UpdatedAt,
                o.DeletedAt))
            .ToList();

        var result = new PagedResult<OrderDto>(pagedOrders, totalCount, page, pageSize);
        return FluentResults.Result.Ok(result);
    }
}

public class GetOrderReceiptHandler : IRequestHandler<GetOrderReceiptQuery, FluentResults.Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderReceiptHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderReceiptQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        var dto = new OrderDto(
            order.Id,
            order.ShopId,
            order.StaffId,
            order.OrderNumber,
            new CustomerInfoDto(order.CustomerInfo.Name, order.CustomerInfo.Contact, (CustomerType)order.CustomerInfo.Type),
            order.Items.Select(i => new OrderItemDto(
                i.MenuItemId,
                i.Name,
                i.Description,
                new MoneyDto(i.UnitPrice.Amount, i.UnitPrice.Currency),
                i.Quantity,
                i.SpecialInstructions ?? string.Empty,
                new MoneyDto(i.TotalPrice.Amount, i.TotalPrice.Currency))).ToList(),
            order.Status,
            order.PaymentStatus,
            new MoneyDto(order.TotalAmount.Amount, order.TotalAmount.Currency),
            order.OrderTime,
            order.CompletedTime,
            order.CreatedAt,
            order.UpdatedAt,
            order.DeletedAt);

        return FluentResults.Result.Ok(dto);
    }
}
