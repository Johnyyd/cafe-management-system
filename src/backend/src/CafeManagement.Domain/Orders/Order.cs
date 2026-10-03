using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Orders.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.ObjectModel;

namespace CafeManagement.Domain.Orders;

public class OrderItem
{
    public ObjectId MenuItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Money UnitPrice { get; init; } = default!;
    public int Quantity { get; init; }
    public string SpecialInstructions { get; init; } = string.Empty;
    public Money TotalPrice { get; init; } = default!;

    private OrderItem() { }

    public OrderItem(ObjectId menuItemId, string name, string description, Money unitPrice, int quantity,
        string specialInstructions = "")
    {
        if (menuItemId == ObjectId.Empty)
            throw new ArgumentException("MenuItemId cannot be empty", nameof(menuItemId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (unitPrice.Amount <= 0)
            throw new ArgumentException("UnitPrice must be greater than zero", nameof(unitPrice));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        MenuItemId = menuItemId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UnitPrice = unitPrice;
        Quantity = quantity;
        SpecialInstructions = specialInstructions?.Trim() ?? string.Empty;
        TotalPrice = unitPrice.Multiply(quantity);
    }
}

public class Order : AggregateRoot<ObjectId>
{
    public ObjectId ShopId { get; private set; }
    public ObjectId StaffId { get; private set; }
    public CustomerInfo CustomerInfo { get; private set; } = default!;
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public Money TotalAmount { get; private set; } = default!;
    public DateTime OrderTime { get; private set; }
    public DateTime? CompletedTime { get; private set; }

    private readonly List<OrderItem> _items = new();

    private Order() { }

    private Order(ObjectId id, ObjectId shopId, ObjectId staffId, CustomerInfo customerInfo,
        IEnumerable<OrderItem> items, DateTime orderTime, ObjectId? createdBy = null)
        : base(id)
    {
        ShopId = shopId;
        StaffId = staffId;
        CustomerInfo = customerInfo;
        _items.AddRange(items);
        OrderTime = orderTime;
        Status = OrderStatus.Placed;
        PaymentStatus = PaymentStatus.Pending;
        SetAuditInfo(createdBy);
        RecalculateTotalAmount();
        AddDomainEvent(new OrderPlacedEvent(Id, ShopId, StaffId, CustomerInfo, Items, orderTime, createdBy));
    }

    public static Result<Order> Create(ObjectId shopId, ObjectId staffId, CustomerInfo customerInfo,
        IEnumerable<OrderItem> items, DateTime? orderTime = null, ObjectId? createdBy = null)
    {
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (staffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("StaffId"));
        if (customerInfo == null)
            return Result.Fail(DomainErrors.Validation.Required("CustomerInfo"));
        if (!customerInfo.IsValid)
            return Result.Fail(DomainErrors.Validation.Required("CustomerInfo"));
        if (items == null || !items.Any())
            return Result.Fail(DomainErrors.Validation.Required("Items"));
        if (items.Any(i => i.MenuItemId == ObjectId.Empty))
            return Result.Fail(DomainErrors.Validation.Required("MenuItemId in Items"));
        if (items.Any(i => i.Quantity <= 0))
            return Result.Fail(DomainErrors.Validation.OutOfRange("Quantity", 0, "unlimited"));
        if (items.Any(i => i.UnitPrice.Amount <= 0))
            return Result.Fail(DomainErrors.Validation.OutOfRange("UnitPrice", 0, "unlimited"));

        var orderTimeValue = orderTime ?? DateTime.UtcNow;

        var order = new Order(
            ObjectId.GenerateNewId(),
            shopId,
            staffId,
            customerInfo,
            items,
            orderTimeValue,
            createdBy);

        return Result.Ok(order);
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _items.Aggregate(Money.Zero(), (total, item) => total.Add(item.TotalPrice));
    }

    public Result UpdateCustomerInfo(CustomerInfo customerInfo, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted order"));
        if (!OrderStatusExtensions.CanBeModified(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));
        if (customerInfo == null || !customerInfo.IsValid)
            return Result.Fail(DomainErrors.Validation.Required("CustomerInfo"));

        CustomerInfo = customerInfo;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new OrderCustomerInfoUpdatedEvent(Id, customerInfo, updatedBy));
        return Result.Ok();
    }

    public Result AddItem(OrderItem item, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot modify a deleted order"));
        if (!OrderStatusExtensions.CanBeModified(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));
        if (item == null)
            return Result.Fail(DomainErrors.Validation.Required("Item"));
        if (item.MenuItemId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("MenuItemId"));
        if (item.Quantity <= 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Quantity", 0, "unlimited"));
        if (item.UnitPrice.Amount <= 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("UnitPrice", 0, "unlimited"));

        _items.Add(item);
        RecalculateTotalAmount();
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new OrderItemAddedEvent(Id, item, updatedBy));
        return Result.Ok();
    }

    public Result RemoveItem(ObjectId menuItemId, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot modify a deleted order"));
        if (!OrderStatusExtensions.CanBeModified(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));
        if (menuItemId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("MenuItemId"));

        var existingItem = _items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (existingItem == null)
            return Result.Fail(DomainErrors.General.NotFound("OrderItem", menuItemId));

        _items.Remove(existingItem);
        RecalculateTotalAmount();
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new OrderItemRemovedEvent(Id, menuItemId, updatedBy));
        return Result.Ok();
    }

    public Result UpdateItemQuantity(ObjectId menuItemId, int newQuantity, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot modify a deleted order"));
        if (!OrderStatusExtensions.CanBeModified(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));
        if (menuItemId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("MenuItemId"));
        if (newQuantity <= 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Quantity", 0, "unlimited"));

        var existingItem = _items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (existingItem == null)
            return Result.Fail(DomainErrors.General.NotFound("OrderItem", menuItemId));

        // We can't modify the item directly since it's immutable, so remove and add new
        _items.Remove(existingItem);
        var updatedItem = new OrderItem(
            existingItem.MenuItemId,
            existingItem.Name,
            existingItem.Description,
            existingItem.UnitPrice,
            newQuantity,
            existingItem.SpecialInstructions);
        _items.Add(updatedItem);

        RecalculateTotalAmount();
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new OrderItemQuantityUpdatedEvent(Id, menuItemId, newQuantity, updatedBy));
        return Result.Ok();
    }

    public Result UpdateItemSpecialInstructions(ObjectId menuItemId, string specialInstructions, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot modify a deleted order"));
        if (!OrderStatusExtensions.CanBeModified(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));
        if (menuItemId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("MenuItemId"));

        var existingItem = _items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (existingItem == null)
            return Result.Fail(DomainErrors.General.NotFound("OrderItem", menuItemId));

        // Create new item with updated instructions
        _items.Remove(existingItem);
        var updatedItem = new OrderItem(
            existingItem.MenuItemId,
            existingItem.Name,
            existingItem.Description,
            existingItem.UnitPrice,
            existingItem.Quantity,
            specialInstructions);
        _items.Add(updatedItem);

        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new OrderItemSpecialInstructionsUpdatedEvent(Id, menuItemId, specialInstructions, updatedBy));
        return Result.Ok();
    }

    public Result ChangeStatus(OrderStatus newStatus, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot change status of a deleted order"));
        if (!OrderStatusExtensions.IsTerminal(Status) && !OrderStatusExtensions.CanBeModified(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));
        if (!Enum.IsDefined(typeof(OrderStatus), newStatus))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("Status", "Placed, Preparing, Ready, Completed, Cancelled"));
        if (Status == newStatus)
            return Result.Ok();

        // Business rules for status transitions
        if (newStatus == OrderStatus.Completed && PaymentStatus != PaymentStatus.Paid)
            return Result.Fail(DomainErrors.Business.PaymentRequired());

        var oldStatus = Status;
        Status = newStatus;
        SetAuditInfo(updatedBy: updatedBy);

        if (newStatus == OrderStatus.Completed)
        {
            CompletedTime = DateTime.UtcNow;
            AddDomainEvent(new OrderCompletedEvent(Id, updatedBy));
        }
        else if (newStatus == OrderStatus.Cancelled)
        {
            AddDomainEvent(new OrderCancelledEvent(Id, updatedBy));
        }
        else
        {
            AddDomainEvent(new OrderStatusChangedEvent(Id, oldStatus, newStatus, updatedBy));
        }

        return Result.Ok();
    }

    public Result ProcessPayment(PaymentStatus newPaymentStatus, ObjectId? processedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot process payment for a deleted order"));
        if (!Enum.IsDefined(typeof(PaymentStatus), newPaymentStatus))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("PaymentStatus", "Pending, Paid, Failed, Refunded"));
        if (PaymentStatus == newPaymentStatus)
            return Result.Ok();

        var oldPaymentStatus = PaymentStatus;
        PaymentStatus = newPaymentStatus;
        SetAuditInfo(updatedBy: processedBy);

        if (newPaymentStatus == PaymentStatus.Paid)
        {
            AddDomainEvent(new OrderPaymentProcessedEvent(Id, TotalAmount, processedBy));
        }
        else if (newPaymentStatus == PaymentStatus.Failed)
        {
            AddDomainEvent(new OrderPaymentFailedEvent(Id, processedBy));
        }
        else if (newPaymentStatus == PaymentStatus.Refunded)
        {
            AddDomainEvent(new OrderRefundedEvent(Id, TotalAmount, processedBy));
        }

        return Result.Ok();
    }

    public Result Cancel(ObjectId? cancelledBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a deleted order"));
        if (!OrderStatusExtensions.CanBeCancelled(Status))
            return Result.Fail(DomainErrors.Business.OrderCannotBeModified($"Order status is {Status}"));

        Status = OrderStatus.Cancelled;
        SetAuditInfo(updatedBy: cancelledBy);
        AddDomainEvent(new OrderCancelledEvent(Id, cancelledBy));
        return Result.Ok();
    }

    public Result Deactivate(ObjectId? deletedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Order is already deleted"));

        MarkAsDeleted(deletedBy);
        AddDomainEvent(new OrderDeactivatedEvent(Id, deletedBy));
        return Result.Ok();
    }
}