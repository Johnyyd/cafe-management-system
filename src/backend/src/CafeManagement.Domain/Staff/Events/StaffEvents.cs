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
    public ObjectId ShopId { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public StaffHiredEvent(ObjectId staffId, string firstName, string lastName, StaffRole role, ObjectId shopId, ObjectId? createdBy = null)
    {
        StaffId = staffId;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        ShopId = shopId;
        CreatedBy = createdBy;
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
    }
}