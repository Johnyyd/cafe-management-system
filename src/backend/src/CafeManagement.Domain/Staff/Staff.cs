using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Shops;
using CafeManagement.Domain.Staff;
using CafeManagement.Domain.Shared;
using CafeManagement.Domain.Staff.Events;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.ObjectModel;

namespace CafeManagement.Domain.Staff;

public class Staff : AggregateRoot<ObjectId>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public StaffRole Role { get; private set; }
    public ContactInfo Contact { get; private set; } = default!;
    public EmploymentStatus EmploymentStatus { get; private set; } = EmploymentStatus.Active;
    public DateTime HireDate { get; private set; }
    [BsonElement("ShopAssignments")]
    public List<StaffShopAssignment> ShopAssignments { get; private set; } = new();

    private Staff() { }

    private Staff(ObjectId id, string firstName, string lastName, StaffRole role, ContactInfo contact,
        EmploymentStatus employmentStatus, DateTime hireDate, ObjectId? createdBy = null)
        : base(id)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Role = role;
        Contact = contact;
        EmploymentStatus = employmentStatus;
        HireDate = hireDate;
        SetAuditInfo(createdBy);
        AddDomainEvent(new StaffHiredEvent(Id, FirstName, LastName, Role, createdBy));
    }

    public static Result<Staff> Hire(string firstName, string lastName, StaffRole role, ContactInfo contact,
        DateTime hireDate, ObjectId? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Fail(DomainErrors.Validation.Required("FirstName"));
        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Fail(DomainErrors.Validation.Required("LastName"));
        if (!Enum.IsDefined(typeof(StaffRole), role))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("Role", "Barista, Cashier, Manager, Admin"));
        if (contact == null || !contact.IsValid)
            return Result.Fail(DomainErrors.Validation.Required("Contact"));
        if (hireDate > DateTime.UtcNow)
            return Result.Fail(DomainErrors.Validation.OutOfRange("HireDate", "past", DateTime.UtcNow));

        var staff = new Staff(
            ObjectId.GenerateNewId(),
            firstName.Trim(),
            lastName.Trim(),
            role,
            contact,
            EmploymentStatus.Active,
            hireDate,
            createdBy);

        return Result.Ok(staff);
    }

    public Result Update(string firstName, string lastName, ContactInfo contact, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted staff member"));
        if (EmploymentStatus == EmploymentStatus.Terminated)
            return Result.Fail(DomainErrors.Business.StaffNotActive($"{FirstName} {LastName}"));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Fail(DomainErrors.Validation.Required("FirstName"));
        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Fail(DomainErrors.Validation.Required("LastName"));
        if (contact == null || !contact.IsValid)
            return Result.Fail(DomainErrors.Validation.Required("Contact"));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Contact = contact;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new StaffUpdatedEvent(Id, FirstName, LastName, updatedBy));
        return Result.Ok();
    }

    public Result AssignToShop(ObjectId shopId, bool isPrimary, ObjectId? assignedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot assign a deleted staff member to a shop"));

        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));

        // Check if already assigned to this shop and active using helper method
        if (IsAssignedToShop(shopId))
            return HandleExistingAssignment(shopId, isPrimary, assignedBy);

        // If setting as primary, unset other primary assignments first
        if (isPrimary)
            UnsetOtherPrimaryAssignments(shopId, assignedBy);

        var assignmentResult = StaffShopAssignment.Assign(Id, shopId, isPrimary, assignedBy);
        if (assignmentResult.IsFailed)
            return Result.Fail(assignmentResult.Errors);

        var assignment = assignmentResult.Value;
        ShopAssignments.Add(assignment);
        AddDomainEvent(new StaffShopAssignedEvent(assignment.Id, Id, shopId, assignment.AssignedDate, isPrimary, assignedBy));

        return Result.Ok();
    }

    private Result HandleExistingAssignment(ObjectId shopId, bool isPrimary, ObjectId? assignedBy)
    {
        // If already assigned, just update primary status if needed
        var existingAssignment = ShopAssignments.FirstOrDefault(a => a.ShopId == shopId && a.IsActive);
        if (existingAssignment == null)
            return Result.Fail(DomainErrors.Validation.NotFound("StaffShopAssignment", $"Staff {Id} is not actively assigned to shop {shopId}"));

        if (existingAssignment.IsPrimary == isPrimary)
            return Result.Ok(); // Already assigned with correct primary status

        // If setting as primary, unset other primary assignments first
        if (isPrimary)
            UnsetOtherPrimaryAssignments(shopId, assignedBy);

        return existingAssignment.SetAsPrimary(assignedBy);
    }

    private void UnsetOtherPrimaryAssignments(ObjectId shopId, ObjectId? updatedBy = null)
    {
        foreach (var otherAssignment in ShopAssignments.Where(a => a.IsActive && a.IsPrimary && a.ShopId != shopId))
        {
            otherAssignment.RemoveAsPrimary(updatedBy);
        }
    }

    public Result UnassignFromShop(ObjectId shopId, ObjectId? unassignedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot unassign a deleted staff member from a shop"));
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));

        var assignment = ShopAssignments.FirstOrDefault(a => a.ShopId == shopId && a.IsActive);
        if (assignment == null)
            return Result.Fail(DomainErrors.Validation.NotFound("StaffShopAssignment", $"Staff {Id} is not actively assigned to shop {shopId}"));

        var result = assignment.Unassign(unassignedBy);
        if (result.IsFailed)
            return result;

        AddDomainEvent(new StaffShopUnassignedEvent(assignment.Id, Id, shopId, assignment.AssignedDate, assignment.UnassignedDate!.Value, assignment.IsPrimary, unassignedBy));
        return Result.Ok();
    }

    public Result SetPrimaryShop(ObjectId shopId, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot set primary shop for a deleted staff member"));
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));

        var assignment = ShopAssignments.FirstOrDefault(a => a.ShopId == shopId && a.IsActive);
        if (assignment == null)
            return Result.Fail(DomainErrors.Validation.NotFound("StaffShopAssignment", $"Staff {Id} is not actively assigned to shop {shopId}"));

        var result = assignment.SetAsPrimary(updatedBy);
        if (result.IsFailed)
            return result;

        // Unset all other assignments as non-primary
        foreach (var otherAssignment in ShopAssignments.Where(a => a.ShopId != shopId && a.IsActive))
        {
            otherAssignment.RemoveAsPrimary(updatedBy);
        }

        AddDomainEvent(new StaffShopAssignmentUpdatedEvent(assignment.Id, Id, shopId, assignment.AssignedDate, assignment.UnassignedDate, false, true, updatedBy));
        return Result.Ok();
    }

    public ObjectId? GetPrimaryShopId()
    {
        var primaryAssignment = ShopAssignments.FirstOrDefault(a => a.IsActive && a.IsPrimary);
        return primaryAssignment?.ShopId;
    }

    public IReadOnlyList<ObjectId> GetAssignedShopIds()
    {
        return ShopAssignments.Where(a => a.IsActive).Select(a => a.ShopId).ToList();
    }

    public bool IsAssignedToShop(ObjectId shopId)
    {
        return ShopAssignments.Any(a => a.ShopId == shopId && a.IsActive);
    }

    public Result ChangeRole(StaffRole newRole, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot change role of a deleted staff member"));
        if (EmploymentStatus != EmploymentStatus.Active)
            return Result.Fail(DomainErrors.Business.StaffNotActive($"{FirstName} {LastName}"));
        if (!Enum.IsDefined(typeof(StaffRole), newRole))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("Role", "Barista, Cashier, Manager, Admin"));
        if (Role == newRole)
            return Result.Ok();

        var oldRole = Role;
        Role = newRole;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new StaffRoleChangedEvent(Id, oldRole, newRole, updatedBy));
        return Result.Ok();
    }

    public Result ChangeEmploymentStatus(EmploymentStatus newStatus, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot change status of a deleted staff member"));
        if (EmploymentStatus == newStatus)
            return Result.Ok();
        if (!Enum.IsDefined(typeof(EmploymentStatus), newStatus))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("EmploymentStatus", "Active, OnLeave, Terminated"));

        var oldStatus = EmploymentStatus;
        EmploymentStatus = newStatus;
        SetAuditInfo(updatedBy: updatedBy);

        if (newStatus == EmploymentStatus.Terminated)
        {
            AddDomainEvent(new StaffTerminatedEvent(Id, FirstName, LastName, updatedBy));
        }
        else
        {
            AddDomainEvent(new StaffEmploymentStatusChangedEvent(Id, oldStatus, newStatus, updatedBy));
        }

        return Result.Ok();
    }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive => EmploymentStatus == EmploymentStatus.Active && !IsDeleted;
}