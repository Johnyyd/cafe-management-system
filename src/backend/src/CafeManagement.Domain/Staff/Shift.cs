using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Staff;

public class Shift : AggregateRoot<ObjectId>
{
    public ObjectId StaffId { get; private set; }
    public ObjectId ShopId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public ShiftStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public ObjectId? CoveredByStaffId { get; private set; }
    public DateTime? CoveredAt { get; private set; }
    public ObjectId? CoveredBy { get; private set; }
    public DateTime? SwappedAt { get; private set; }
    public ObjectId? SwappedWithStaffId { get; private set; }
    public ObjectId? SwappedWithShiftId { get; private set; }

    private Shift() { }

    private Shift(ObjectId id, ObjectId staffId, ObjectId shopId, DateTime startTime, DateTime endTime, string? notes, ObjectId? createdBy = null)
        : base(id)
    {
        StaffId = staffId;
        ShopId = shopId;
        StartTime = startTime;
        EndTime = endTime;
        Status = ShiftStatus.Scheduled;
        Notes = notes;
        SetAuditInfo(createdBy);
        AddDomainEvent(new ShiftScheduledEvent(Id, StaffId, ShopId, StartTime, EndTime, createdBy));
    }

    public static Result<Shift> Schedule(ObjectId staffId, ObjectId shopId, DateTime startTime, DateTime endTime, string? notes = null, ObjectId? createdBy = null)
    {
        if (staffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("StaffId"));
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (startTime >= endTime)
            return Result.Fail(DomainErrors.Validation.OutOfRange("StartTime", "before EndTime", endTime));
        if (endTime <= DateTime.UtcNow)
            return Result.Fail(DomainErrors.Validation.OutOfRange("EndTime", "future", DateTime.UtcNow));
        if (endTime - startTime > TimeSpan.FromHours(16))
            return Result.Fail(DomainErrors.Validation.OutOfRange("ShiftDuration", "16 hours maximum", TimeSpan.FromHours(16)));

        var shift = new Shift(
            ObjectId.GenerateNewId(),
            staffId,
            shopId,
            startTime,
            endTime,
            notes,
            createdBy);

        return Result.Ok(shift);
    }

    public Result Start(ObjectId? startedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot start a deleted shift"));
        if (Status != ShiftStatus.Scheduled)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot start shift with status {Status}"));
        if (StartTime > DateTime.UtcNow.AddMinutes(15))
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot start shift more than 15 minutes before scheduled start time"));

        Status = ShiftStatus.InProgress;
        SetAuditInfo(updatedBy: startedBy);
        AddDomainEvent(new ShiftStartedEvent(Id, StaffId, ShopId, StartTime, startedBy));

        return Result.Ok();
    }

    public Result Complete(ObjectId? completedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot complete a deleted shift"));
        if (Status != ShiftStatus.InProgress)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot complete shift with status {Status}"));
        if (EndTime < DateTime.UtcNow.AddMinutes(-15))
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot complete shift more than 15 minutes after scheduled end time"));

        Status = ShiftStatus.Completed;
        SetAuditInfo(updatedBy: completedBy);
        AddDomainEvent(new ShiftCompletedEvent(Id, StaffId, ShopId, EndTime, completedBy));

        return Result.Ok();
    }

    public Result Cancel(string? reason = null, ObjectId? cancelledBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a deleted shift"));
        if (Status == ShiftStatus.Completed)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a completed shift"));
        if (Status == ShiftStatus.Cancelled)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shift is already cancelled"));

        var oldStatus = Status;
        Status = ShiftStatus.Cancelled;
        Notes = reason ?? Notes;
        SetAuditInfo(updatedBy: cancelledBy);
        AddDomainEvent(new ShiftCancelledEvent(Id, StaffId, ShopId, oldStatus, reason, cancelledBy));

        return Result.Ok();
    }

    public Result Cover(ObjectId coveredByStaffId, ObjectId? coveredBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cover a deleted shift"));
        if (Status != ShiftStatus.Scheduled && Status != ShiftStatus.InProgress)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot cover shift with status {Status}"));
        if (CoveredByStaffId.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shift is already covered"));

        CoveredByStaffId = coveredByStaffId;
        CoveredAt = DateTime.UtcNow;
        CoveredBy = coveredBy;
        Status = ShiftStatus.Covered;
        SetAuditInfo(updatedBy: coveredBy);
        AddDomainEvent(new ShiftCoveredEvent(Id, StaffId, coveredByStaffId, CoveredAt.Value, coveredBy));

        return Result.Ok();
    }

    public Result Uncover(ObjectId? uncoveredBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot uncover a deleted shift"));
        if (Status != ShiftStatus.Covered)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shift is not covered"));
        if (!CoveredByStaffId.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("No cover assignment to remove"));

        var oldCoveredByStaffId = CoveredByStaffId.Value;
        CoveredByStaffId = null;
        CoveredAt = null;
        CoveredBy = null;
        Status = ShiftStatus.Scheduled;
        SetAuditInfo(updatedBy: uncoveredBy);
        AddDomainEvent(new ShiftUncoveredEvent(Id, StaffId, oldCoveredByStaffId, uncoveredBy));

        return Result.Ok();
    }

    public Result Swap(ObjectId swappedWithShiftId, ObjectId swappedWithStaffId, ObjectId? swappedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot swap a deleted shift"));
        if (Status != ShiftStatus.Scheduled)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot swap shift with status {Status}"));
        if (SwappedWithShiftId.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shift is already swapped"));

        SwappedWithShiftId = swappedWithShiftId;
        SwappedWithStaffId = swappedWithStaffId;
        SwappedAt = DateTime.UtcNow;
        SetAuditInfo(updatedBy: swappedBy);
        AddDomainEvent(new ShiftSwappedEvent(Id, StaffId, swappedWithShiftId, swappedWithStaffId, swappedBy));

        return Result.Ok();
    }

    public Result Unswap(ObjectId? unswappedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot unswap a deleted shift"));
        if (!SwappedWithShiftId.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shift is not swapped"));
        if (!SwappedWithStaffId.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shift swap has no staff reference"));

        var oldSwappedWithShiftId = SwappedWithShiftId.Value;
        var oldSwappedWithStaffId = SwappedWithStaffId.Value;
        SwappedWithShiftId = null;
        SwappedWithStaffId = null;
        SwappedAt = null;
        SetAuditInfo(updatedBy: unswappedBy);
        AddDomainEvent(new ShiftUnswappedEvent(Id, StaffId, oldSwappedWithShiftId, oldSwappedWithStaffId, unswappedBy));

        return Result.Ok();
    }

    public Result UpdateNotes(string? notes, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update notes on a deleted shift"));

        Notes = notes;
        SetAuditInfo(updatedBy: updatedBy);
        return Result.Ok();
    }

    public TimeSpan Duration => EndTime - StartTime;
    public bool IsActive => Status == ShiftStatus.Scheduled || Status == ShiftStatus.InProgress;
    public bool IsCovered => CoveredByStaffId.HasValue;
    public bool IsSwapped => SwappedWithShiftId.HasValue;
    public ObjectId EffectiveStaffId => CoveredByStaffId ?? StaffId;
}

public enum ShiftStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Covered = 4
}