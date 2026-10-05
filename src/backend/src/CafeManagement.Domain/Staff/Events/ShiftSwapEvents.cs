using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;

namespace CafeManagement.Domain.Staff.Events;

public record ShiftSwapRequestedEvent : DomainEvent
{
    public ObjectId ShiftSwapId { get; init; }
    public ObjectId RequestingStaffId { get; init; }
    public ObjectId RequestedStaffId { get; init; }
    public ObjectId RequestingShiftId { get; init; }
    public ObjectId RequestedShiftId { get; init; }
    public ObjectId ShopId { get; init; }
    public string? Reason { get; init; }
    public ObjectId? CreatedBy { get; init; }

    public ShiftSwapRequestedEvent(ObjectId shiftSwapId, ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, string? reason, ObjectId? createdBy = null)
    {
        ShiftSwapId = shiftSwapId;
        RequestingStaffId = requestingStaffId;
        RequestedStaffId = requestedStaffId;
        RequestingShiftId = requestingShiftId;
        RequestedShiftId = requestedShiftId;
        ShopId = shopId;
        Reason = reason;
        CreatedBy = createdBy;
        EventType = nameof(ShiftSwapRequestedEvent);
    }
}

public record ShiftSwapAcceptedEvent : DomainEvent
{
    public ObjectId ShiftSwapId { get; init; }
    public ObjectId RequestingStaffId { get; init; }
    public ObjectId RequestedStaffId { get; init; }
    public ObjectId RequestingShiftId { get; init; }
    public ObjectId RequestedShiftId { get; init; }
    public ObjectId ShopId { get; init; }
    public ObjectId AcceptedBy { get; init; }

    public ShiftSwapAcceptedEvent(ObjectId shiftSwapId, ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, ObjectId acceptedBy)
    {
        ShiftSwapId = shiftSwapId;
        RequestingStaffId = requestingStaffId;
        RequestedStaffId = requestedStaffId;
        RequestingShiftId = requestingShiftId;
        RequestedShiftId = requestedShiftId;
        ShopId = shopId;
        AcceptedBy = acceptedBy;
        EventType = nameof(ShiftSwapAcceptedEvent);
    }
}

public record ShiftSwapRejectedEvent : DomainEvent
{
    public ObjectId ShiftSwapId { get; init; }
    public ObjectId RequestingStaffId { get; init; }
    public ObjectId RequestedStaffId { get; init; }
    public ObjectId RequestingShiftId { get; init; }
    public ObjectId RequestedShiftId { get; init; }
    public ObjectId ShopId { get; init; }
    public ObjectId RejectedBy { get; init; }
    public string RejectionReason { get; init; } = string.Empty;

    public ShiftSwapRejectedEvent(ObjectId shiftSwapId, ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, ObjectId rejectedBy, string rejectionReason)
    {
        ShiftSwapId = shiftSwapId;
        RequestingStaffId = requestingStaffId;
        RequestedStaffId = requestedStaffId;
        RequestingShiftId = requestingShiftId;
        RequestedShiftId = requestedShiftId;
        ShopId = shopId;
        RejectedBy = rejectedBy;
        RejectionReason = rejectionReason;
        EventType = nameof(ShiftSwapRejectedEvent);
    }
}

public record ShiftSwapCancelledEvent : DomainEvent
{
    public ObjectId ShiftSwapId { get; init; }
    public ObjectId RequestingStaffId { get; init; }
    public ObjectId RequestedStaffId { get; init; }
    public ObjectId RequestingShiftId { get; init; }
    public ObjectId RequestedShiftId { get; init; }
    public ObjectId ShopId { get; init; }
    public ShiftSwapStatus OldStatus { get; init; }
    public ObjectId? CancelledBy { get; init; }

    public ShiftSwapCancelledEvent(ObjectId shiftSwapId, ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, ShiftSwapStatus oldStatus, ObjectId? cancelledBy = null)
    {
        ShiftSwapId = shiftSwapId;
        RequestingStaffId = requestingStaffId;
        RequestedStaffId = requestedStaffId;
        RequestingShiftId = requestingShiftId;
        RequestedShiftId = requestedShiftId;
        ShopId = shopId;
        OldStatus = oldStatus;
        CancelledBy = cancelledBy;
        EventType = nameof(ShiftSwapCancelledEvent);
    }
}

public record ShiftSwapApprovedEvent : DomainEvent
{
    public ObjectId ShiftSwapId { get; init; }
    public ObjectId RequestingStaffId { get; init; }
    public ObjectId RequestedStaffId { get; init; }
    public ObjectId RequestingShiftId { get; init; }
    public ObjectId RequestedShiftId { get; init; }
    public ObjectId ShopId { get; init; }
    public ObjectId ApprovedBy { get; init; }
    public string? ApprovalNotes { get; init; }

    public ShiftSwapApprovedEvent(ObjectId shiftSwapId, ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, ObjectId approvedBy, string? approvalNotes = null)
    {
        ShiftSwapId = shiftSwapId;
        RequestingStaffId = requestingStaffId;
        RequestedStaffId = requestedStaffId;
        RequestingShiftId = requestingShiftId;
        RequestedShiftId = requestedShiftId;
        ShopId = shopId;
        ApprovedBy = approvedBy;
        ApprovalNotes = approvalNotes;
        EventType = nameof(ShiftSwapApprovedEvent);
    }
}