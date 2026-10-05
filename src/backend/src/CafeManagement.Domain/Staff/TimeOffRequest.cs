using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Staff;

public class TimeOffRequest : AggregateRoot<ObjectId>
{
    public ObjectId StaffId { get; private set; }
    public ObjectId ShopId { get; private set; }
    public TimeOffType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public TimeOffStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public ObjectId? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovalNotes { get; private set; }
    public ObjectId? RejectedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private TimeOffRequest() { }

    private TimeOffRequest(ObjectId id, ObjectId staffId, ObjectId shopId, TimeOffType type, DateTime startDate, DateTime endDate, string? reason, ObjectId? createdBy = null)
        : base(id)
    {
        StaffId = staffId;
        ShopId = shopId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Status = TimeOffStatus.Pending;
        Reason = reason;
        SetAuditInfo(createdBy);
        AddDomainEvent(new TimeOffRequestSubmittedEvent(Id, StaffId, ShopId, Type, StartDate, EndDate, Reason, createdBy));
    }

    public static Result<TimeOffRequest> Submit(ObjectId staffId, ObjectId shopId, TimeOffType type, DateTime startDate, DateTime endDate, string? reason = null, ObjectId? createdBy = null)
    {
        if (staffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("StaffId"));
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (startDate >= endDate)
            return Result.Fail(DomainErrors.Validation.OutOfRange("StartDate", "before EndDate", endDate));
        if (startDate.Date < DateTime.UtcNow.Date)
            return Result.Fail(DomainErrors.Validation.OutOfRange("StartDate", "today or future", DateTime.UtcNow.Date));
        if (endDate - startDate > TimeSpan.FromDays(365))
            return Result.Fail(DomainErrors.Validation.OutOfRange("TimeOffDuration", "365 days maximum", TimeSpan.FromDays(365)));

        var request = new TimeOffRequest(
            ObjectId.GenerateNewId(),
            staffId,
            shopId,
            type,
            startDate,
            endDate,
            reason,
            createdBy);

        return Result.Ok(request);
    }

    public Result Approve(ObjectId approvedBy, string? approvalNotes = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot approve a deleted time-off request"));
        if (Status != TimeOffStatus.Pending)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot approve request with status {Status}"));

        Status = TimeOffStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        ApprovalNotes = approvalNotes;
        SetAuditInfo(updatedBy: approvedBy);
        AddDomainEvent(new TimeOffRequestApprovedEvent(Id, StaffId, ShopId, Type, StartDate, EndDate, approvedBy, approvalNotes));

        return Result.Ok();
    }

    public Result Reject(ObjectId rejectedBy, string rejectionReason)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot reject a deleted time-off request"));
        if (Status != TimeOffStatus.Pending)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot reject request with status {Status}"));
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Fail(DomainErrors.Validation.Required("RejectionReason"));

        Status = TimeOffStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason.Trim();
        SetAuditInfo(updatedBy: rejectedBy);
        AddDomainEvent(new TimeOffRequestRejectedEvent(Id, StaffId, ShopId, Type, StartDate, EndDate, rejectedBy, rejectionReason));

        return Result.Ok();
    }

    public Result Cancel(ObjectId? cancelledBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a deleted time-off request"));
        if (Status == TimeOffStatus.Cancelled)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Request is already cancelled"));
        if (Status == TimeOffStatus.Approved && StartDate <= DateTime.UtcNow)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel approved time-off that has already started"));

        var oldStatus = Status;
        Status = TimeOffStatus.Cancelled;
        SetAuditInfo(updatedBy: cancelledBy);
        AddDomainEvent(new TimeOffRequestCancelledEvent(Id, StaffId, ShopId, oldStatus, cancelledBy));

        return Result.Ok();
    }

    public Result UpdateReason(string? reason, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted time-off request"));
        if (Status != TimeOffStatus.Pending)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot update reason for request with status {Status}"));

        Reason = reason;
        SetAuditInfo(updatedBy: updatedBy);
        return Result.Ok();
    }

    public Result UpdateDates(DateTime newStartDate, DateTime newEndDate, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted time-off request"));
        if (Status != TimeOffStatus.Pending)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot update dates for request with status {Status}"));
        if (newStartDate >= newEndDate)
            return Result.Fail(DomainErrors.Validation.OutOfRange("StartDate", "before EndDate", newEndDate));
        if (newStartDate.Date < DateTime.UtcNow.Date)
            return Result.Fail(DomainErrors.Validation.OutOfRange("StartDate", "today or future", DateTime.UtcNow.Date));

        StartDate = newStartDate;
        EndDate = newEndDate;
        SetAuditInfo(updatedBy: updatedBy);
        return Result.Ok();
    }

    public int DurationDays => (int)Math.Ceiling((EndDate - StartDate).TotalDays);
    public bool IsPending => Status == TimeOffStatus.Pending;
    public bool IsApproved => Status == TimeOffStatus.Approved;
    public bool IsActive => Status == TimeOffStatus.Approved && StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow;
}

public enum TimeOffType
{
    Vacation = 0,
    SickLeave = 1,
    Personal = 2,
    Unpaid = 3,
    Bereavement = 4,
    JuryDuty = 5,
    Other = 6
}

public enum TimeOffStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}