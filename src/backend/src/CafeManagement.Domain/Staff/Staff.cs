using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Shops;
using CafeManagement.Domain.Staff.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Staff;

public class Staff : AggregateRoot<ObjectId>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public StaffRole Role { get; private set; }
    public ContactInfo Contact { get; private set; } = default!;
    public EmploymentStatus EmploymentStatus { get; private set; } = EmploymentStatus.Active;
    public DateTime HireDate { get; private set; }
    public ObjectId ShopId { get; private set; }

    private Staff() { }

    private Staff(ObjectId id, string firstName, string lastName, StaffRole role, ContactInfo contact,
        EmploymentStatus employmentStatus, DateTime hireDate, ObjectId shopId, ObjectId? createdBy = null)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        Contact = contact;
        EmploymentStatus = employmentStatus;
        HireDate = hireDate;
        ShopId = shopId;
        SetAuditInfo(createdBy);
        AddDomainEvent(new StaffHiredEvent(Id, FirstName, LastName, Role, ShopId, createdBy));
    }

    public static Result<Staff> Hire(string firstName, string lastName, StaffRole role, ContactInfo contact,
        DateTime hireDate, ObjectId shopId, ObjectId? createdBy = null)
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
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));

        var staff = new Staff(
            ObjectId.GenerateNewId(),
            firstName.Trim(),
            lastName.Trim(),
            role,
            contact,
            EmploymentStatus.Active,
            hireDate,
            shopId,
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

    public Result TransferToShop(ObjectId newShopId, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot transfer a deleted staff member"));
        if (newShopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (ShopId == newShopId)
            return Result.Ok();

        var oldShopId = ShopId;
        ShopId = newShopId;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new StaffTransferredEvent(Id, oldShopId, newShopId, updatedBy));
        return Result.Ok();
    }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive => EmploymentStatus == EmploymentStatus.Active && !IsDeleted;
}