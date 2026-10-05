using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Shift.Events;
using CafeManagement.Domain.Shared;
using CafeManagement.Domain.Staff;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Shift;

public class Shift : AggregateRoot<ObjectId>
{
    public ObjectId StaffId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public ShiftStatus Status { get; private set; }
    public ShiftType Type { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private Shift() { }

    private Shift(ObjectId id, ObjectId staffId, DateTime startTime, DateTime endTime,
        ShiftStatus status, ShiftType type, string notes, ObjectId? createdBy = null)
        : base(id)
    {
        StaffId = staffId;
        StartTime = startTime;
        EndTime = endTime;
        Status = status;
        Type = type;
        Notes = notes?.Trim() ?? string.Empty;
        SetAuditInfo(createdBy);
        AddDomainEvent(new ShiftCreatedEvent(Id, StaffId, StartTime, EndTime, Type, createdBy));
    }

    public static Result<Shift> Create(ObjectId staffId, DateTime startTime, DateTime endTime,
        ShiftType type, string notes, ObjectId? createdBy = null)
    {
        if (staffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("StaffId"));
        if (startTime >= endTime)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Shift timing", "StartTime must be before EndTime", "unlimited"));
        if (startTime.Date != endTime.Date)
            return Result.Fail(DomainErrors.Validation.InvalidFormat("Shift timing", "Shift must be within a single day"));
        if (!Enum.IsDefined(typeof(ShiftType), type))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("Type", "Regular, Overtime, Training, OnCall"));
        if (string.IsNullOrWhiteSpace(notes))
            notes = string.Empty;

        var shift = new Shift(
            ObjectId.GenerateNewId(),
            staffId,
            startTime,
            endTime,
            ShiftStatus.Scheduled,
            type,
            notes,
            createdBy);

        return Result.Ok(shift);
    }

    public Result Update(DateTime startTime, DateTime endTime, ShiftType type, string notes, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted shift"));
        if (Status == ShiftStatus.Completed || Status == ShiftStatus.Cancelled)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a completed or cancelled shift"));
        if (startTime >= endTime)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Shift timing", "StartTime must be before EndTime", "unlimited"));
        if (startTime.Date != endTime.Date)
            return Result.Fail(DomainErrors.Validation.InvalidFormat("Shift timing", "Shift must be within a single day"));
        if (!Enum.IsDefined(typeof(ShiftType), type))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("Type", "Regular, Overtime, Training, OnCall"));

        var oldStartTime = StartTime;
        var oldEndTime = EndTime;
        var oldType = Type;
        var oldNotes = Notes;

        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        Notes = notes?.Trim() ?? string.Empty;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new ShiftUpdatedEvent(Id, StaffId, oldStartTime, oldEndTime, oldType, oldNotes, StartTime, EndTime, Type, Notes, updatedBy));

        return Result.Ok();
    }

    public Result StartShift(ObjectId? startedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot start a deleted shift"));
        if (Status != ShiftStatus.Scheduled)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot start a shift with status {Status}"));
        if (StartTime > DateTime.UtcNow)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot start a shift before its scheduled time"));

        var oldStatus = Status;
        Status = ShiftStatus.InProgress;
        SetAuditInfo(updatedBy: startedBy);
        AddDomainEvent(new ShiftStartedEvent(Id, StaffId, StartTime, EndTime, startedBy));

        return Result.Ok();
    }

    public Result EndShift(ObjectId? endedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot end a deleted shift"));
        if (Status != ShiftStatus.InProgress)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot end a shift with status {Status}"));

        var oldStatus = Status;
        Status = ShiftStatus.Completed;
        SetAuditInfo(updatedBy: endedBy);
        AddDomainEvent(new ShiftEndedEvent(Id, StaffId, StartTime, EndTime, endedBy));

        return Result.Ok();
    }

    public Result CancelShift(ObjectId? cancelledBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a deleted shift"));
        if (Status == ShiftStatus.Completed)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a completed shift"));

        var oldStatus = Status;
        Status = ShiftStatus.Cancelled;
        SetAuditInfo(updatedBy: cancelledBy);
        AddDomainEvent(new ShiftCancelledEvent(Id, StaffId, StartTime, EndTime, cancelledBy));

        return Result.Ok();
    }

    public Result RequestTimeOff(ObjectId? requestedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot request time off for a deleted shift"));
        if (Status != ShiftStatus.Scheduled)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot request time off for a shift with status {Status}"));

        var oldStatus = Status;
        Status = ShiftStatus.TimeOffRequested;
        SetAuditInfo(updatedBy: requestedBy);
        AddDomainEvent(new ShiftTimeOffRequestedEvent(Id, StaffId, StartTime, EndTime, requestedBy));

        return Result.Ok();
    }

    public Result ApproveTimeOff(ObjectId? approvedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot approve time off for a deleted shift"));
        if (Status != ShiftStatus.TimeOffRequested)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot approve time off for a shift with status {Status}"));

        var oldStatus = Status;
        Status = ShiftStatus.TimeOffApproved;
        SetAuditInfo(updatedBy: approvedBy);
        AddDomainEvent(new ShiftTimeOffApprovedEvent(Id, StaffId, StartTime, EndTime, approvedBy));

        return Result.Ok();
    }

    public Result DenyTimeOff(ObjectId? deniedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot deny time off for a deleted shift"));
        if (Status != ShiftStatus.TimeOffRequested)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot deny time off for a shift with status {Status}"));

        var oldStatus = Status;
        Status = ShiftStatus.Scheduled; // Back to scheduled
        SetAuditInfo(updatedBy: deniedBy);
        AddDomainEvent(new ShiftTimeOffDeniedEvent(Id, StaffId, StartTime, EndTime, deniedBy));

        return Result.Ok();
    }

    public Result SwapShift(ObjectId newStaffId, ObjectId? swappedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot swap a deleted shift"));
        if (newStaffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("StaffId"));
        if (Status != ShiftStatus.Scheduled)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot swap a shift with status {Status}"));

        var oldStaffId = StaffId;
        StaffId = newStaffId;
        SetAuditInfo(updatedBy: swappedBy);
        AddDomainEvent(new ShiftSwappedEvent(Id, oldStaffId, newStaffId, StartTime, EndTime, swappedBy));

        return Result.Ok();
    }

    public string FullDescription => $"{Type} shift from {StartTime:yyyy-MM-dd HH:mm} to {EndTime:yyyy-MM-dd HH:mm}";
    public bool IsActive => Status == ShiftStatus.Scheduled || Status == ShiftStatus.InProgress;
    public bool IsCompleted => Status == ShiftStatus.Completed;
    public bool IsCancelled => Status == ShiftStatus.Cancelled;
}

public enum ShiftStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    TimeOffRequested = 5,
    TimeOffApproved = 6,
    TimeOffDenied = 7
}

public enum ShiftType
{
    Regular = 1,
    Overtime = 2,
    Training = 3,
    OnCall = 4
}