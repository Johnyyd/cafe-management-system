using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Orders.Queries;
using CafeManagement.Domain.Common;
using DomainOrders = CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Orders.Handlers;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<UpdateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order updated: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelOrderHandler> _logger;

    public CancelOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<CancelOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order cancelled: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<ProcessPaymentHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FluentResults.Result> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
            return FluentResults.Result.Fail(DomainErrors.General.NotFound("Order", request.Id));

        var paymentStatus = (DomainOrders.PaymentStatus)request.PaymentMethod;

        var result = order.ProcessPayment(paymentStatus);
        if (result.IsFailed)
            return FluentResults.Result.Fail(result.Errors);

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order payment processed: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderStatusHandler> _logger;

    public UpdateOrderStatusHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<UpdateOrderStatusHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order status updated: {OrderId} to {Status}", order.Id, request.Status);
        return FluentResults.Result.Ok();
    }
}

public class AddOrderItemHandler : IRequestHandler<AddOrderItemCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOrderItemHandler> _logger;

    public AddOrderItemHandler(
        IOrderRepository orderRepository,
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddOrderItemHandler> logger)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
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

        if (!menuItem.IsAvailableAt(DateTime.UtcNow))
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order item added to order: {OrderId}", order.Id);
        return FluentResults.Result.Ok();
    }
}

public class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderItemHandler> _logger;

    public UpdateOrderItemHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<UpdateOrderItemHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order item updated: {OrderItemId} in order: {OrderId}", request.OrderItemId, request.OrderId);
        return FluentResults.Result.Ok();
    }
}

public class RemoveOrderItemHandler : IRequestHandler<RemoveOrderItemCommand, FluentResults.Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveOrderItemHandler> _logger;

    public RemoveOrderItemHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<RemoveOrderItemHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var totalCount = orders.Count;
        var pagedOrders = orders
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.ShopId,
                o.StaffId,
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

        var result = new PagedResult<OrderDto>(pagedOrders, totalCount, request.Page, request.PageSize);
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
