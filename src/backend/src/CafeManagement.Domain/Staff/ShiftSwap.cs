using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Staff;

public class ShiftSwap : AggregateRoot<ObjectId>
{
    public ObjectId RequestingStaffId { get; private set; }
    public ObjectId RequestedStaffId { get; private set; }
    public ObjectId RequestingShiftId { get; private set; }
    public ObjectId RequestedShiftId { get; private set; }
    public ObjectId ShopId { get; private set; }
    public ShiftSwapStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public ObjectId? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovalNotes { get; private set; }
    public ObjectId? RejectedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public ObjectId? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private ShiftSwap() { }

    private ShiftSwap(ObjectId id, ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, string? reason, ObjectId? createdBy = null)
        : base(id)
    {
        RequestingStaffId = requestingStaffId;
        RequestedStaffId = requestedStaffId;
        RequestingShiftId = requestingShiftId;
        RequestedShiftId = requestedShiftId;
        ShopId = shopId;
        Status = ShiftSwapStatus.Pending;
        Reason = reason;
        SetAuditInfo(createdBy);
        AddDomainEvent(new ShiftSwapRequestedEvent(Id, RequestingStaffId, RequestedStaffId, RequestingShiftId, RequestedShiftId, ShopId, reason, createdBy));
    }

    public static Result<ShiftSwap> Request(ObjectId requestingStaffId, ObjectId requestedStaffId, ObjectId requestingShiftId, ObjectId requestedShiftId, ObjectId shopId, string? reason = null, ObjectId? createdBy = null)
    {
        if (requestingStaffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("RequestingStaffId"));
        if (requestedStaffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("RequestedStaffId"));
        if (requestingShiftId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("RequestingShiftId"));
        if (requestedShiftId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("RequestedShiftId"));
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (requestingStaffId == requestedStaffId)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot request swap with yourself"));

        var swap = new ShiftSwap(
            ObjectId.GenerateNewId(),
            requestingStaffId,
            requestedStaffId,
            requestingShiftId,
            requestedShiftId,
            shopId,
            reason,
            createdBy);

        return Result.Ok(swap);
    }

    public Result Accept(ObjectId acceptedBy)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot accept a deleted shift swap"));
        if (Status != ShiftSwapStatus.Pending)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot accept swap with status {Status}"));
        if (acceptedBy != RequestedStaffId && acceptedBy != RequestingStaffId)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Only the requesting or requested staff can accept this swap"));

        Status = ShiftSwapStatus.Accepted;
        ApprovedBy = acceptedBy;
        ApprovedAt = DateTime.UtcNow;
        SetAuditInfo(updatedBy: acceptedBy);
        AddDomainEvent(new ShiftSwapAcceptedEvent(Id, RequestingStaffId, RequestedStaffId, RequestingShiftId, RequestedShiftId, ShopId, acceptedBy));

        return Result.Ok();
    }

    public Result Reject(ObjectId rejectedBy, string rejectionReason)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot reject a deleted shift swap"));
        if (Status != ShiftSwapStatus.Pending && Status != ShiftSwapStatus.Accepted)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot reject swap with status {Status}"));
        if (rejectedBy != RequestedStaffId && rejectedBy != RequestingStaffId)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Only the requesting or requested staff can reject this swap"));
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Fail(DomainErrors.Validation.Required("RejectionReason"));

        Status = ShiftSwapStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason.Trim();
        SetAuditInfo(updatedBy: rejectedBy);
        AddDomainEvent(new ShiftSwapRejectedEvent(Id, RequestingStaffId, RequestedStaffId, RequestingShiftId, RequestedShiftId, ShopId, rejectedBy, rejectionReason));

        return Result.Ok();
    }

    public Result Cancel(ObjectId? cancelledBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel a deleted shift swap"));
        if (Status == ShiftSwapStatus.Cancelled)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Swap is already cancelled"));
        if (Status == ShiftSwapStatus.Approved)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot cancel an approved swap"));

        var oldStatus = Status;
        Status = ShiftSwapStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        SetAuditInfo(updatedBy: cancelledBy);
        AddDomainEvent(new ShiftSwapCancelledEvent(Id, RequestingStaffId, RequestedStaffId, RequestingShiftId, RequestedShiftId, ShopId, oldStatus, cancelledBy));

        return Result.Ok();
    }

    public Result Approve(ObjectId approvedBy, string? approvalNotes = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot approve a deleted shift swap"));
        if (Status != ShiftSwapStatus.Accepted)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot approve swap with status {Status}"));

        Status = ShiftSwapStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        ApprovalNotes = approvalNotes;
        SetAuditInfo(updatedBy: approvedBy);
        AddDomainEvent(new ShiftSwapApprovedEvent(Id, RequestingStaffId, RequestedStaffId, RequestingShiftId, RequestedShiftId, ShopId, approvedBy, approvalNotes));

        return Result.Ok();
    }

    public Result UpdateReason(string? reason, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted shift swap"));
        if (Status != ShiftSwapStatus.Pending)
            return Result.Fail(DomainErrors.Business.InvalidOperation($"Cannot update reason for swap with status {Status}"));

        Reason = reason;
        SetAuditInfo(updatedBy: updatedBy);
        return Result.Ok();
    }

    public bool IsPending => Status == ShiftSwapStatus.Pending;
    public bool IsAccepted => Status == ShiftSwapStatus.Accepted;
    public bool IsApproved => Status == ShiftSwapStatus.Approved;
    public bool IsActive => Status == ShiftSwapStatus.Pending || Status == ShiftSwapStatus.Accepted;
}

public enum ShiftSwapStatus
{
    Pending = 0,
    Accepted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}