using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;

namespace CafeManagement.Domain.Shops.Events;

public record ShopCreatedEvent : DomainEvent
{
    public ObjectId ShopId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ObjectId? CreatedBy { get; init; }

    public ShopCreatedEvent(ObjectId shopId, string name, ObjectId? createdBy = null)
    {
        ShopId = shopId;
        Name = name;
        CreatedBy = createdBy;
    }
}

public record ShopUpdatedEvent : DomainEvent
{
    public ObjectId ShopId { get; init; }
    public string OldName { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
    public ObjectId? UpdatedBy { get; init; }

    public ShopUpdatedEvent(ObjectId shopId, string oldName, string newName, ObjectId? updatedBy = null)
    {
        ShopId = shopId;
        OldName = oldName;
        NewName = newName;
        UpdatedBy = updatedBy;
    }
}

public record ShopOperatingHoursUpdatedEvent : DomainEvent
{
    public ObjectId ShopId { get; init; }
    public IReadOnlyList<OperatingHours> OperatingHours { get; init; } = Array.Empty<OperatingHours>();
    public ObjectId? UpdatedBy { get; init; }

    public ShopOperatingHoursUpdatedEvent(ObjectId shopId, IEnumerable<OperatingHours> operatingHours, ObjectId? updatedBy = null)
    {
        ShopId = shopId;
        OperatingHours = operatingHours.ToList().AsReadOnly();
        UpdatedBy = updatedBy;
    }
}

public record ShopStatusChangedEvent : DomainEvent
{
    public ObjectId ShopId { get; init; }
    public ShopStatus OldStatus { get; init; }
    public ShopStatus NewStatus { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public ShopStatusChangedEvent(ObjectId shopId, ShopStatus oldStatus, ShopStatus newStatus, ObjectId? updatedBy = null)
    {
        ShopId = shopId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        UpdatedBy = updatedBy;
    }
}

public record ShopDeactivatedEvent : DomainEvent
{
    public ObjectId ShopId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ObjectId? DeletedBy { get; init; }

    public ShopDeactivatedEvent(ObjectId shopId, string name, ObjectId? deletedBy = null)
    {
        ShopId = shopId;
        Name = name;
        DeletedBy = deletedBy;
    }
}