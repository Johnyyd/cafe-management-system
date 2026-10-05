using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;

namespace CafeManagement.Domain.Staff.Events;

public record TimeOffRequestSubmittedEvent : DomainEvent
{
    public ObjectId TimeOffRequestId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public TimeOffType Type { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Reason { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public TimeOffRequestSubmittedEvent(ObjectId timeOffRequestId, ObjectId staffId, ObjectId shopId, TimeOffType type, DateTime startDate, DateTime endDate, string? reason, ObjectId? createdBy = null)
    {
        TimeOffRequestId = timeOffRequestId;
        StaffId = staffId;
        ShopId = shopId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason;
        CreatedBy = createdBy;
        EventType = nameof(TimeOffRequestSubmittedEvent);
    }
}

public record TimeOffRequestApprovedEvent : DomainEvent
{
    public ObjectId TimeOffRequestId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public TimeOffType Type { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public ObjectId ApprovedBy { get; init; }
    public string? ApprovalNotes { get; init; }

    public TimeOffRequestApprovedEvent(ObjectId timeOffRequestId, ObjectId staffId, ObjectId shopId, TimeOffType type, DateTime startDate, DateTime endDate, ObjectId approvedBy, string? approvalNotes = null)
    {
        TimeOffRequestId = timeOffRequestId;
        StaffId = staffId;
        ShopId = shopId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        ApprovedBy = approvedBy;
        ApprovalNotes = approvalNotes;
        EventType = nameof(TimeOffRequestApprovedEvent);
    }
}

public record TimeOffRequestRejectedEvent : DomainEvent
{
    public ObjectId TimeOffRequestId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public TimeOffType Type { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public ObjectId RejectedBy { get; init; }
    public string RejectionReason { get; init; } = string.Empty;

    public TimeOffRequestRejectedEvent(ObjectId timeOffRequestId, ObjectId staffId, ObjectId shopId, TimeOffType type, DateTime startDate, DateTime endDate, ObjectId rejectedBy, string rejectionReason)
    {
        TimeOffRequestId = timeOffRequestId;
        StaffId = staffId;
        ShopId = shopId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        RejectedBy = rejectedBy;
        RejectionReason = rejectionReason;
        EventType = nameof(TimeOffRequestRejectedEvent);
    }
}

public record TimeOffRequestCancelledEvent : DomainEvent
{
    public ObjectId TimeOffRequestId { get; init; }
    public ObjectId StaffId { get; init; }
    public ObjectId ShopId { get; init; }
    public TimeOffStatus OldStatus { get; init; }
    public ObjectId? CancelledBy { get; init; }

    public TimeOffRequestCancelledEvent(ObjectId timeOffRequestId, ObjectId staffId, ObjectId shopId, TimeOffStatus oldStatus, ObjectId? cancelledBy = null)
    {
        TimeOffRequestId = timeOffRequestId;
        StaffId = staffId;
        ShopId = shopId;
        OldStatus = oldStatus;
        CancelledBy = cancelledBy;
        EventType = nameof(TimeOffRequestCancelledEvent);
    }
}