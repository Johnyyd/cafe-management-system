using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;

namespace CafeManagement.Domain.Staff.Events;

public record StaffHiredEvent : DomainEvent
{
    public ObjectId StaffId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public StaffRole Role { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public StaffHiredEvent(ObjectId staffId, string firstName, string lastName, StaffRole role, ObjectId? createdBy = null)
    {
        StaffId = staffId;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        CreatedBy = createdBy;
        EventType = nameof(StaffHiredEvent);
    }
}

public record StaffUpdatedEvent : DomainEvent
{
    public ObjectId StaffId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public ObjectId? UpdatedBy { get; init; }

    public StaffUpdatedEvent(ObjectId staffId, string firstName, string lastName, ObjectId? updatedBy = null)
    {
        StaffId = staffId;
        FirstName = firstName;
        LastName = lastName;
        UpdatedBy = updatedBy;
        EventType = nameof(StaffUpdatedEvent);
    }
}

public record StaffRoleChangedEvent : DomainEvent
{
    public ObjectId StaffId { get; init; }
    public StaffRole OldRole { get; init; }
    public StaffRole NewRole { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public StaffRoleChangedEvent(ObjectId staffId, StaffRole oldRole, StaffRole newRole, ObjectId? updatedBy = null)
    {
        StaffId = staffId;
        OldRole = oldRole;
        NewRole = newRole;
        UpdatedBy = updatedBy;
        EventType = nameof(StaffRoleChangedEvent);
    }
}

public record StaffEmploymentStatusChangedEvent : DomainEvent
{
    public ObjectId StaffId { get; init; }
    public EmploymentStatus OldStatus { get; init; }
    public EmploymentStatus NewStatus { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public StaffEmploymentStatusChangedEvent(ObjectId staffId, EmploymentStatus oldStatus, EmploymentStatus newStatus, ObjectId? updatedBy = null)
    {
        StaffId = staffId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        UpdatedBy = updatedBy;
        EventType = nameof(StaffEmploymentStatusChangedEvent);
    }
}

public record StaffTerminatedEvent : DomainEvent
{
    public ObjectId StaffId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public ObjectId? TerminatedBy { get; init; }

    public StaffTerminatedEvent(ObjectId staffId, string firstName, string lastName, ObjectId? terminatedBy = null)
    {
        StaffId = staffId;
        FirstName = firstName;
        LastName = lastName;
        TerminatedBy = terminatedBy;
        EventType = nameof(StaffTerminatedEvent);
    }
}

public record StaffTransferredEvent : DomainEvent
{
    public ObjectId StaffId { get; init; }
    public ObjectId OldShopId { get; init; }
    public ObjectId NewShopId { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public StaffTransferredEvent(ObjectId staffId, ObjectId oldShopId, ObjectId newShopId, ObjectId? updatedBy = null)
    {
        StaffId = staffId;
        OldShopId = oldShopId;
        NewShopId = newShopId;
        UpdatedBy = updatedBy;
        EventType = nameof(StaffTransferredEvent);
    }
}

public record StaffShopAssignedEvent : DomainEvent
{
    public ObjectId EventId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public DateTime AssignedDate { get; init; }
    public bool IsPrimary { get; init; }
    public ObjectId? AssignedBy { get; init; }

    public StaffShopAssignedEvent(ObjectId eventId, ObjectId staffId, ObjectId shopId, DateTime assignedDate, bool isPrimary, ObjectId? assignedBy = null)
    {
        EventId = eventId;
        StaffId = staffId;
        ShopId = shopId;
        AssignedDate = assignedDate;
        IsPrimary = isPrimary;
        AssignedBy = assignedBy;
        EventType = nameof(StaffShopAssignedEvent);
    }
}

public record StaffShopUnassignedEvent : DomainEvent
{
    public ObjectId EventId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public DateTime AssignedDate { get; init; }
    public DateTime UnassignedDate { get; init; }
    public bool WasPrimary { get; init; }
    public ObjectId? UnassignedBy { get; init; }

    public StaffShopUnassignedEvent(ObjectId eventId, ObjectId staffId, ObjectId shopId, DateTime assignedDate, DateTime unassignedDate, bool wasPrimary, ObjectId? unassignedBy = null)
    {
        EventId = eventId;
        StaffId = staffId;
        ShopId = shopId;
        AssignedDate = assignedDate;
        UnassignedDate = unassignedDate;
        WasPrimary = wasPrimary;
        UnassignedBy = unassignedBy;
        EventType = nameof(StaffShopUnassignedEvent);
    }
}

public record StaffShopAssignmentUpdatedEvent : DomainEvent
{
    public ObjectId EventId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public DateTime AssignedDate { get; init; }
    public DateTime? UnassignedDate { get; init; }
    public bool WasPrimary { get; init; }
    public bool IsPrimary { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public StaffShopAssignmentUpdatedEvent(ObjectId eventId, ObjectId staffId, ObjectId shopId, DateTime assignedDate, DateTime? unassignedDate, bool wasPrimary, bool isPrimary, ObjectId? updatedBy = null)
    {
        EventId = eventId;
        StaffId = staffId;
        ShopId = shopId;
        AssignedDate = assignedDate;
        UnassignedDate = unassignedDate;
        WasPrimary = wasPrimary;
        IsPrimary = isPrimary;
        UpdatedBy = updatedBy;
        EventType = nameof(StaffShopAssignmentUpdatedEvent);
    }
}