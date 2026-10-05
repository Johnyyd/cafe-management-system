using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;

namespace CafeManagement.Domain.Shift.Events;

public record ShiftCreatedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ShiftType Type { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public ShiftCreatedEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ShiftType type, ObjectId? createdBy = null)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        CreatedBy = createdBy;
        EventType = nameof(ShiftCreatedEvent);
    }
}

public record ShiftUpdatedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime OldStartTime { get; init; }
    public DateTime OldEndTime { get; init; }
    public ShiftType OldType { get; init; }
    public string OldNotes { get; init; }
    public DateTime NewStartTime { get; init; }
    public DateTime NewEndTime { get; init; }
    public ShiftType NewType { get; init; }
    public string NewNotes { get; init; }
    public ObjectId? UpdatedBy { get; init; }

    public ShiftUpdatedEvent(
        ObjectId shiftId,
        ObjectId staffId,
        DateTime oldStartTime,
        DateTime oldEndTime,
        ShiftType oldType,
        string oldNotes,
        DateTime newStartTime,
        DateTime newEndTime,
        ShiftType newType,
        string newNotes,
        ObjectId? updatedBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        OldStartTime = oldStartTime;
        OldEndTime = oldEndTime;
        OldType = oldType;
        OldNotes = oldNotes;
        NewStartTime = newStartTime;
        NewEndTime = newEndTime;
        NewType = newType;
        NewNotes = newNotes;
        UpdatedBy = updatedBy;
        EventType = nameof(ShiftUpdatedEvent);
    }
}

public record ShiftStartedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? StartedBy { get; init; }

    public ShiftStartedEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ObjectId? startedBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        StartedBy = startedBy;
        EventType = nameof(ShiftStartedEvent);
    }
}

public record ShiftEndedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? EndedBy { get; init; }

    public ShiftEndedEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ObjectId? endedBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        EndedBy = endedBy;
        EventType = nameof(ShiftEndedEvent);
    }
}

public record ShiftCancelledEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? CancelledBy { get; init; }

    public ShiftCancelledEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ObjectId? cancelledBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        CancelledBy = cancelledBy;
        EventType = nameof(ShiftCancelledEvent);
    }
}

public record ShiftTimeOffRequestedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? RequestedBy { get; init; }

    public ShiftTimeOffRequestedEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ObjectId? requestedBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        RequestedBy = requestedBy;
        EventType = nameof(ShiftTimeOffRequestedEvent);
    }
}

public record ShiftTimeOffApprovedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? ApprovedBy { get; init; }

    public ShiftTimeOffApprovedEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ObjectId? approvedBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        ApprovedBy = approvedBy;
        EventType = nameof(ShiftTimeOffApprovedEvent);
    }
}

public record ShiftTimeOffDeniedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId StaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? DeniedBy { get; init; }

    public ShiftTimeOffDeniedEvent(ObjectId shiftId, ObjectId staffId, DateTime startTime, DateTime endTime, ObjectId? deniedBy)
    {
        ShiftId = shiftId;
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        DeniedBy = deniedBy;
        EventType = nameof(ShiftTimeOffDeniedEvent);
    }
}

public record ShiftSwappedEvent : DomainEvent
{
    public ObjectId ShiftId { get; init; }
    public ObjectId OldStaffId { get; init; }
    public ObjectId NewStaffId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public ObjectId? SwappedBy { get; init; }

    public ShiftSwappedEvent(ObjectId shiftId, ObjectId oldStaffId, ObjectId newStaffId, DateTime startTime, DateTime endTime, ObjectId? swappedBy)
    {
        ShiftId = shiftId;
        OldStaffId = oldStaffId;
        NewStaffId = newStaffId;
        StartTime = startTime;
        EndTime = endTime;
        SwappedBy = swappedBy;
        EventType = nameof(ShiftSwappedEvent);
    }
}