using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;

namespace CafeManagement.Domain.Staff.Events;

public record ShiftScheduledEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public ShiftScheduledEvent(ObjectId shiftId, ObjectId staffId, ObjectId shopId, DateTime startTime, DateTime endTime, ObjectId? createdBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        ShopId = shopId;
        StartTime = startTime;
        EndTime = endTime;
        CreatedBy = createdBy;
        EventType = nameof(ShiftScheduledEvent);
    }
}

public record ShiftStartedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public DateTime StartTime { get; init; }
    public ObjectId? StartedBy { get; init; }

    public ShiftStartedEvent(ObjectId shiftId, ObjectId staffId, ObjectId shopId, DateTime startTime, ObjectId? startedBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        ShopId = shopId;
        StartTime = startTime;
        StartedBy = startedBy;
        EventType = nameof(ShiftStartedEvent);
    }
}

public record ShiftCompletedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? CompletedBy { get; init; }

    public ShiftCompletedEvent(ObjectId shiftId, ObjectId staffId, ObjectId shopId, DateTime endTime, ObjectId? completedBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        ShopId = shopId;
        EndTime = endTime;
        CompletedBy = completedBy;
        EventType = nameof(ShiftCompletedEvent);
    }
}

public record ShiftCancelledEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public ShiftStatus OldStatus { get; init; }
    public string? Reason { get; init; }
    public ObjectId? CancelledBy { get; init; }

    public ShiftCancelledEvent(ObjectId shiftId, ObjectId staffId, ObjectId shopId, ShiftStatus oldStatus, string? reason, ObjectId? cancelledBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        ShopId = shopId;
        OldStatus = oldStatus;
        Reason = reason;
        CancelledBy = cancelledBy;
        EventType = nameof(ShiftCancelledEvent);
    }
}

public record ShiftCoveredEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId CoveredByStaffId { get; init; }
    public DateTime CoveredAt { get; init; }
    public ObjectId? CoveredBy { get; init; }

    public ShiftCoveredEvent(ObjectId shiftId, ObjectId staffId, ObjectId coveredByStaffId, DateTime coveredAt, ObjectId? coveredBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        CoveredByStaffId = coveredByStaffId;
        CoveredAt = coveredAt;
        CoveredBy = coveredBy;
        EventType = nameof(ShiftCoveredEvent);
    }
}

public record ShiftUncoveredEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId PreviouslyCoveredByStaffId { get; init; }
    public ObjectId? UncoveredBy { get; init; }

    public ShiftUncoveredEvent(ObjectId shiftId, ObjectId staffId, ObjectId previouslyCoveredByStaffId, ObjectId? uncoveredBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        PreviouslyCoveredByStaffId = previouslyCoveredByStaffId;
        UncoveredBy = uncoveredBy;
        EventType = nameof(ShiftUncoveredEvent);
    }
}

public record ShiftSwappedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId SwappedWithShiftId { get; init; }
    public ObjectId SwappedWithStaffId { get; init; }
    public ObjectId? SwappedBy { get; init; }

    public ShiftSwappedEvent(ObjectId shiftId, ObjectId staffId, ObjectId swappedWithShiftId, ObjectId swappedWithStaffId, ObjectId? swappedBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        SwappedWithShiftId = swappedWithShiftId;
        SwappedWithStaffId = swappedWithStaffId;
        SwappedBy = swappedBy;
        EventType = nameof(ShiftSwappedEvent);
    }
}

public record ShiftUnswappedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId PreviouslySwappedWithShiftId { get; init; }
    public ObjectId PreviouslySwappedWithStaffId { get; init; }
    public ObjectId? UnswappedBy { get; init; }

    public ShiftUnswappedEvent(ObjectId shiftId, ObjectId staffId, ObjectId previouslySwappedWithShiftId, ObjectId previouslySwappedWithStaffId, ObjectId? unswappedBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        PreviouslySwappedWithShiftId = previouslySwappedWithShiftId;
        PreviouslySwappedWithStaffId = previouslySwappedWithStaffId;
        UnswappedBy = unswappedBy;
        EventType = nameof(ShiftUnswappedEvent);
    }
}