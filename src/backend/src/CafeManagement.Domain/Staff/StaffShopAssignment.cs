using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Staff.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Staff;

public class StaffShopAssignment : AggregateRoot<ObjectId>
{
    public ObjectId StaffId { get; private set; }
    public ObjectId ShopId { get; private set; }
    public DateTime AssignedDate { get; private set; }
    public DateTime? UnassignedDate { get; private set; }
    public bool IsPrimary { get; private set; }

    private StaffShopAssignment() { }

    private StaffShopAssignment(ObjectId id, ObjectId staffId, ObjectId shopId,
        DateTime assignedDate, bool isPrimary, ObjectId? createdBy = null)
        : base(id)
    {
        StaffId = staffId;
        ShopId = shopId;
        AssignedDate = assignedDate;
        UnassignedDate = null;
        IsPrimary = isPrimary;
        SetAuditInfo(createdBy);
        AddDomainEvent(new StaffShopAssignedEvent(Id, StaffId, ShopId, AssignedDate, IsPrimary, createdBy));
    }

    public static Result<StaffShopAssignment> Assign(ObjectId staffId, ObjectId shopId,
        bool isPrimary, ObjectId? createdBy = null)
    {
        if (staffId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("StaffId"));
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));

        var assignment = new StaffShopAssignment(
            ObjectId.GenerateNewId(),
            staffId,
            shopId,
            DateTime.UtcNow,
            isPrimary,
            createdBy);

        return Result.Ok(assignment);
    }

    public Result Unassign(ObjectId? unassignedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot unassign a deleted staff-shop assignment"));
        if (UnassignedDate.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Staff-shop assignment is already unassigned"));

        var oldIsPrimary = IsPrimary;
        UnassignedDate = DateTime.UtcNow;
        SetAuditInfo(updatedBy: unassignedBy);
        AddDomainEvent(new StaffShopUnassignedEvent(Id, StaffId, ShopId, AssignedDate, UnassignedDate.Value, oldIsPrimary, unassignedBy));

        return Result.Ok();
    }

    public Result SetAsPrimary(ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot set primary on a deleted staff-shop assignment"));
        if (UnassignedDate.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot set primary on an unassigned staff-shop assignment"));

        IsPrimary = true;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new StaffShopAssignmentUpdatedEvent(Id, StaffId, ShopId, AssignedDate, UnassignedDate, false, true, updatedBy));

        return Result.Ok();
    }

    public Result RemoveAsPrimary(ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot remove primary on a deleted staff-shop assignment"));
        if (UnassignedDate.HasValue)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot remove primary on an unassigned staff-shop assignment"));

        IsPrimary = false;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new StaffShopAssignmentUpdatedEvent(Id, StaffId, ShopId, AssignedDate, UnassignedDate, true, false, updatedBy));

        return Result.Ok();
    }

    public bool IsActive => !UnassignedDate.HasValue && !IsDeleted;
    public DateTime? EffectiveEndDate => UnassignedDate;
}