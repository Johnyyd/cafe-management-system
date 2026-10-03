using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Shops.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Shops;

public class Shop : AggregateRoot<ObjectId>
{
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = default!;
    public ContactInfo Contact { get; private set; } = default!;
    public IReadOnlyList<OperatingHours> OperatingHours => _operatingHours.AsReadOnly();
    public ShopStatus Status { get; private set; } = ShopStatus.Active;

    private readonly List<OperatingHours> _operatingHours = new();

    private Shop() { }

    private Shop(ObjectId id, string name, Address address, ContactInfo contact, IEnumerable<OperatingHours> operatingHours, ObjectId? createdBy = null)
        : base(id)
    {
        Name = name;
        Address = address;
        Contact = contact;
        _operatingHours.AddRange(operatingHours);
        Status = ShopStatus.Active;
        SetAuditInfo(createdBy);
        AddDomainEvent(new ShopCreatedEvent(Id, Name, createdBy));
    }

    public static Result<Shop> Create(string name, Address address, ContactInfo contact, IEnumerable<OperatingHours> operatingHours, ObjectId? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainErrors.Validation.Required("Name"));

        if (address == null)
            return Result.Fail(DomainErrors.Validation.Required("Address"));

        if (contact == null || !contact.IsValid)
            return Result.Fail(DomainErrors.Validation.Required("Contact"));

        var shop = new Shop(ObjectId.GenerateNewId(), name.Trim(), address, contact, operatingHours, createdBy);
        return Result.Ok(shop);
    }

    public Result Update(string name, Address address, ContactInfo contact, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted shop"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainErrors.Validation.Required("Name"));

        if (address == null)
            return Result.Fail(DomainErrors.Validation.Required("Address"));

        if (contact == null || !contact.IsValid)
            return Result.Fail(DomainErrors.Validation.Required("Contact"));

        var oldName = Name;
        Name = name.Trim();
        Address = address;
        Contact = contact;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new ShopUpdatedEvent(Id, oldName, Name, updatedBy));
        return Result.Ok();
    }

    public Result UpdateOperatingHours(IEnumerable<OperatingHours> operatingHours, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted shop"));

        if (operatingHours == null)
            return Result.Fail(DomainErrors.Validation.Required("OperatingHours"));

        var hoursList = operatingHours.ToList();
        if (hoursList.Count == 0)
            return Result.Fail(DomainErrors.Validation.Required("OperatingHours"));

        // Check for duplicate days
        var duplicateDays = hoursList.GroupBy(h => h.DayOfWeek).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateDays.Count > 0)
            return Result.Fail(DomainErrors.Validation.InvalidFormat("OperatingHours", $"Duplicate days: {string.Join(", ", duplicateDays)}"));

        _operatingHours.Clear();
        _operatingHours.AddRange(hoursList);
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new ShopOperatingHoursUpdatedEvent(Id, hoursList, updatedBy));
        return Result.Ok();
    }

    public Result SetStatus(ShopStatus status, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot change status of a deleted shop"));

        if (Status == status)
            return Result.Ok();

        var oldStatus = Status;
        Status = status;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new ShopStatusChangedEvent(Id, oldStatus, status, updatedBy));
        return Result.Ok();
    }

    public Result Deactivate(ObjectId? deletedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Shop is already deleted"));

        MarkAsDeleted(deletedBy);
        AddDomainEvent(new ShopDeactivatedEvent(Id, Name, deletedBy));
        return Result.Ok();
    }

    public bool IsOpenAt(DateTime dateTime)
    {
        if (Status != ShopStatus.Active)
            return false;

        var dayOfWeek = (int)dateTime.DayOfWeek;
        var timeOfDay = dateTime.TimeOfDay;

        var hours = _operatingHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
        return hours?.IsOpenAt(timeOfDay) ?? false;
    }
}