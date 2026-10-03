using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Domain.Orders.Events;

public record OrderPlacedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId ShopId { get; init; }
    public ObjectId StaffId { get; init; }
    public CustomerInfo CustomerInfo { get; init; } = default!;
    public IReadOnlyList<OrderItem> Items { get; init; } = Array.Empty<OrderItem>();
    public DateTime OrderTime { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public OrderPlacedEvent(ObjectId orderId, ObjectId shopId, ObjectId staffId, CustomerInfo customerInfo,
        IEnumerable<OrderItem> items, DateTime orderTime, ObjectId? createdBy = null)
    {
        OrderId = orderId;
        ShopId = shopId;
        StaffId = staffId;
        CustomerInfo = customerInfo;
        Items = items.ToList().AsReadOnly();
        OrderTime = orderTime;
        CreatedBy = createdBy;
    }
}

public record OrderStatusChangedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public OrderStatus OldStatus { get; init; }
    public OrderStatus NewStatus { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public OrderStatusChangedEvent(ObjectId orderId, OrderStatus oldStatus, OrderStatus newStatus, ObjectId? updatedBy = null)
    {
        OrderId = orderId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        UpdatedBy = updatedBy;
    }
}

public record OrderCustomerInfoUpdatedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public CustomerInfo CustomerInfo { get; init; } = default!;
    public ObjectId? UpdatedBy { get; init; }

    public OrderCustomerInfoUpdatedEvent(ObjectId orderId, CustomerInfo customerInfo, ObjectId? updatedBy = null)
    {
        OrderId = orderId;
        CustomerInfo = customerInfo;
        UpdatedBy = updatedBy;
    }
}

public record OrderItemAddedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public OrderItem Item { get; init; } = default!;
    public ObjectId? UpdatedBy { get; init; }

    public OrderItemAddedEvent(ObjectId orderId, OrderItem item, ObjectId? updatedBy = null)
    {
        OrderId = orderId;
        Item = item;
        UpdatedBy = updatedBy;
    }
}

public record OrderItemRemovedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId MenuItemId { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public OrderItemRemovedEvent(ObjectId orderId, ObjectId menuItemId, ObjectId? updatedBy = null)
    {
        OrderId = orderId;
        MenuItemId = menuItemId;
        UpdatedBy = updatedBy;
    }
}

public record OrderItemQuantityUpdatedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId MenuItemId { get; init; }
    public int NewQuantity { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public OrderItemQuantityUpdatedEvent(ObjectId orderId, ObjectId menuItemId, int newQuantity, ObjectId? updatedBy = null)
    {
        OrderId = orderId;
        MenuItemId = menuItemId;
        NewQuantity = newQuantity;
        UpdatedBy = updatedBy;
    }
}

public record OrderItemSpecialInstructionsUpdatedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId MenuItemId { get; init; }
    public string SpecialInstructions { get; init; } = string.Empty;
    public ObjectId? UpdatedBy { get; init; }

    public OrderItemSpecialInstructionsUpdatedEvent(ObjectId orderId, ObjectId menuItemId, string specialInstructions, ObjectId? updatedBy = null)
    {
        OrderId = orderId;
        MenuItemId = menuItemId;
        SpecialInstructions = specialInstructions;
        UpdatedBy = updatedBy;
    }
}

public record OrderCompletedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public DateTime CompletedTime { get; init; }
    public ObjectId? CompletedBy { get; init; }

    public OrderCompletedEvent(ObjectId orderId, ObjectId? completedBy = null)
    {
        OrderId = orderId;
        CompletedTime = DateTime.UtcNow;
        CompletedBy = completedBy;
    }
}

public record OrderCancelledEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId? CancelledBy { get; init; }

    public OrderCancelledEvent(ObjectId orderId, ObjectId? cancelledBy = null)
    {
        OrderId = orderId;
        CancelledBy = cancelledBy;
    }
}

public record OrderPaymentProcessedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public Money Amount { get; init; } = default!;
    public ObjectId? ProcessedBy { get; init; }

    public OrderPaymentProcessedEvent(ObjectId orderId, Money amount, ObjectId? processedBy = null)
    {
        OrderId = orderId;
        Amount = amount;
        ProcessedBy = processedBy;
    }
}

public record OrderPaymentFailedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId? FailedBy { get; init; }

    public OrderPaymentFailedEvent(ObjectId orderId, ObjectId? failedBy = null)
    {
        OrderId = orderId;
        FailedBy = failedBy;
    }
}

public record OrderRefundedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public Money Amount { get; init; } = default!;
    public ObjectId? RefundedBy { get; init; }

    public OrderRefundedEvent(ObjectId orderId, Money amount, ObjectId? refundedBy = null)
    {
        OrderId = orderId;
        Amount = amount;
        RefundedBy = refundedBy;
    }
}

public record OrderDeactivatedEvent : DomainEvent
{
    public ObjectId OrderId { get; init; }
    public ObjectId? DeletedBy { get; init; }

    public OrderDeactivatedEvent(ObjectId orderId, ObjectId? deletedBy = null)
    {
        OrderId = orderId;
        DeletedBy = deletedBy;
    }
}