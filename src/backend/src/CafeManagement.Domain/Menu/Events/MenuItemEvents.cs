using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Domain.Menu.Events;

public record MenuItemCreatedEvent : DomainEvent
{
    public ObjectId MenuItemId { get; init; }
    public ObjectId ShopId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Money Price { get; init; } = default!;
    public ObjectId? CreatedBy { get; init; }

    public MenuItemCreatedEvent(ObjectId menuItemId, ObjectId shopId, string name, string category, Money price, ObjectId? createdBy = null)
    {
        MenuItemId = menuItemId;
        ShopId = shopId;
        Name = name;
        Category = category;
        Price = price;
        CreatedBy = createdBy;
    }
}

public record MenuItemUpdatedEvent : DomainEvent
{
    public ObjectId MenuItemId { get; init; }
    public ObjectId ShopId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Money Price { get; init; } = default!;
    public ObjectId? UpdatedBy { get; init; }

    public MenuItemUpdatedEvent(ObjectId menuItemId, ObjectId shopId, string name, string category, Money price, ObjectId? updatedBy = null)
    {
        MenuItemId = menuItemId;
        ShopId = shopId;
        Name = name;
        Category = category;
        Price = price;
        UpdatedBy = updatedBy;
    }
}

public record MenuItemAvailabilityChangedEvent : DomainEvent
{
    public ObjectId MenuItemId { get; init; }
    public ObjectId ShopId { get; init; }
    public Availability OldAvailability { get; init; } = default!;
    public Availability NewAvailability { get; init; } = default!;
    public ObjectId? UpdatedBy { get; init; }

    public MenuItemAvailabilityChangedEvent(ObjectId menuItemId, ObjectId shopId, Availability oldAvailability, Availability newAvailability, ObjectId? updatedBy = null)
    {
        MenuItemId = menuItemId;
        ShopId = shopId;
        OldAvailability = oldAvailability;
        NewAvailability = newAvailability;
        UpdatedBy = updatedBy;
    }
}

public record MenuItemStatusChangedEvent : DomainEvent
{
    public ObjectId MenuItemId { get; init; }
    public ObjectId ShopId { get; init; }
    public MenuItemStatus OldStatus { get; init; }
    public MenuItemStatus NewStatus { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public MenuItemStatusChangedEvent(ObjectId menuItemId, ObjectId shopId, MenuItemStatus oldStatus, MenuItemStatus newStatus, ObjectId? updatedBy = null)
    {
        MenuItemId = menuItemId;
        ShopId = shopId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        UpdatedBy = updatedBy;
    }
}

public record MenuItemDeactivatedEvent : DomainEvent
{
    public ObjectId MenuItemId { get; init; }
    public ObjectId ShopId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ObjectId? DeletedBy { get; init; }

    public MenuItemDeactivatedEvent(ObjectId menuItemId, ObjectId shopId, string name, ObjectId? deletedBy = null)
    {
        MenuItemId = menuItemId;
        ShopId = shopId;
        Name = name;
        DeletedBy = deletedBy;
    }
}