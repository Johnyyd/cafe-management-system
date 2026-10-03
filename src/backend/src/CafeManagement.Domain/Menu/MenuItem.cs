using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Menu.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Menu;

public class MenuItem : AggregateRoot<ObjectId>
{
    public ObjectId ShopId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = default!;
    public IReadOnlyList<string> Ingredients => _ingredients.AsReadOnly();
    public IReadOnlyList<string> Allergens => _allergens.AsReadOnly();
    public Availability Availability { get; private set; } = default!;
    public MenuItemStatus Status { get; private set; } = MenuItemStatus.Available;

    private readonly List<string> _ingredients = new();
    private readonly List<string> _allergens = new();

    private MenuItem() { }

    private MenuItem(ObjectId id, ObjectId shopId, string category, string name, string description,
        Money price, IEnumerable<string> ingredients, IEnumerable<string> allergens, Availability availability, ObjectId? createdBy = null)
        : base(id)
    {
        ShopId = shopId;
        Category = category;
        Name = name;
        Description = description;
        Price = price;
        _ingredients.AddRange(ingredients);
        _allergens.AddRange(allergens);
        Availability = availability;
        Status = MenuItemStatus.Available;
        SetAuditInfo(createdBy);
        AddDomainEvent(new MenuItemCreatedEvent(Id, ShopId, Name, Category, Price, createdBy));
    }

    public static Result<MenuItem> Create(ObjectId shopId, string category, string name, string description,
        Money price, IEnumerable<string> ingredients, IEnumerable<string> allergens, Availability availability, ObjectId? createdBy = null)
    {
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (string.IsNullOrWhiteSpace(category))
            return Result.Fail(DomainErrors.Validation.Required("Category"));
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainErrors.Validation.Required("Name"));
        if (price.Amount <= 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Price", 0.01m, "unlimited"));
        if (availability == null)
            return Result.Fail(DomainErrors.Validation.Required("Availability"));

        var item = new MenuItem(
            ObjectId.GenerateNewId(),
            shopId,
            category.Trim(),
            name.Trim(),
            description?.Trim() ?? string.Empty,
            price,
            ingredients?.Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)) ?? Enumerable.Empty<string>(),
            allergens?.Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)) ?? Enumerable.Empty<string>(),
            availability,
            createdBy);

        return Result.Ok(item);
    }

    public Result Update(string category, string name, string description, Money price,
        IEnumerable<string> ingredients, IEnumerable<string> allergens, Availability availability, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted menu item"));

        if (string.IsNullOrWhiteSpace(category))
            return Result.Fail(DomainErrors.Validation.Required("Category"));
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainErrors.Validation.Required("Name"));
        if (price.Amount <= 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Price", 0.01m, "unlimited"));
        if (availability == null)
            return Result.Fail(DomainErrors.Validation.Required("Availability"));

        Category = category.Trim();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        _ingredients.Clear();
        _ingredients.AddRange(ingredients?.Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)) ?? Enumerable.Empty<string>());
        _allergens.Clear();
        _allergens.AddRange(allergens?.Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)) ?? Enumerable.Empty<string>());
        Availability = availability;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new MenuItemUpdatedEvent(Id, ShopId, Name, Category, Price, updatedBy));
        return Result.Ok();
    }

    public Result UpdateAvailability(Availability availability, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted menu item"));
        if (availability == null)
            return Result.Fail(DomainErrors.Validation.Required("Availability"));

        var oldAvailability = Availability;
        Availability = availability;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new MenuItemAvailabilityChangedEvent(Id, ShopId, oldAvailability, availability, updatedBy));
        return Result.Ok();
    }

    public Result SetStatus(MenuItemStatus status, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot change status of a deleted menu item"));
        if (Status == status)
            return Result.Ok();
        if (!Enum.IsDefined(typeof(MenuItemStatus), status))
            return Result.Fail(DomainErrors.Validation.InvalidEnumValue("Status", "Available, Unavailable, Seasonal"));

        var oldStatus = Status;
        Status = status;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new MenuItemStatusChangedEvent(Id, ShopId, oldStatus, status, updatedBy));
        return Result.Ok();
    }

    public Result Deactivate(ObjectId? deletedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Menu item is already deleted"));

        MarkAsDeleted(deletedBy);
        AddDomainEvent(new MenuItemDeactivatedEvent(Id, ShopId, Name, deletedBy));
        return Result.Ok();
    }

    public bool IsAvailableAt(DateTime dateTime)
    {
        if (Status != MenuItemStatus.Available || IsDeleted)
            return false;

        var dayOfWeek = (int)dateTime.DayOfWeek;
        var timeOfDay = dateTime.TimeOfDay;

        if (!Availability.DaysOfWeek.Contains(dayOfWeek))
            return false;

        if (Availability.StartTime.HasValue && timeOfDay < Availability.StartTime.Value)
            return false;

        if (Availability.EndTime.HasValue && timeOfDay > Availability.EndTime.Value)
            return false;

        return true;
    }
}