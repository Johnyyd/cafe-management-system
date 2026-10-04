using CafeManagement.Domain.Common;
using CafeManagement.Domain.Common.Events;
using CafeManagement.Domain.Inventory.Events;
using CafeManagement.Domain.Shared;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Inventory;

public class InventoryItem : AggregateRoot<ObjectId>
{
    public ObjectId ShopId { get; private set; }
    public string ItemName { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public ObjectId? SupplierId { get; private set; }
    public DateTime LastUpdated { get; private set; }

    private InventoryItem() { }

    private InventoryItem(ObjectId id, ObjectId shopId, string itemName, string unit, decimal quantity,
        decimal reorderLevel, ObjectId? supplierId = null, ObjectId? createdBy = null)
        : base(id)
    {
        ShopId = shopId;
        ItemName = itemName;
        Unit = unit;
        Quantity = quantity;
        ReorderLevel = reorderLevel;
        SupplierId = supplierId;
        LastUpdated = DateTime.UtcNow;
        SetAuditInfo(createdBy);
        AddDomainEvent(new InventoryItemCreatedEvent(Id, ShopId, ItemName, Quantity, Unit, createdBy));
    }

    public static Result<InventoryItem> Create(ObjectId shopId, string itemName, string unit, decimal quantity,
        decimal reorderLevel, ObjectId? supplierId = null, ObjectId? createdBy = null)
    {
        if (shopId == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.Required("ShopId"));
        if (string.IsNullOrWhiteSpace(itemName))
            return Result.Fail(DomainErrors.Validation.Required("ItemName"));
        if (string.IsNullOrWhiteSpace(unit))
            return Result.Fail(DomainErrors.Validation.Required("Unit"));
        if (quantity < 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Quantity", 0, "unlimited"));
        if (reorderLevel < 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("ReorderLevel", 0, "unlimited"));
        if (supplierId.HasValue && supplierId.Value == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.InvalidFormat("SupplierId", "Valid ObjectId"));

        var item = new InventoryItem(
            ObjectId.GenerateNewId(),
            shopId,
            itemName.Trim(),
            unit.Trim(),
            quantity,
            reorderLevel,
            supplierId,
            createdBy);

        return Result.Ok(item);
    }

    public Result Update(string itemName, string unit, decimal reorderLevel, ObjectId? supplierId, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot update a deleted inventory item"));

        if (string.IsNullOrWhiteSpace(itemName))
            return Result.Fail(DomainErrors.Validation.Required("ItemName"));
        if (string.IsNullOrWhiteSpace(unit))
            return Result.Fail(DomainErrors.Validation.Required("Unit"));
        if (reorderLevel < 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("ReorderLevel", 0, "unlimited"));
        if (supplierId.HasValue && supplierId.Value == ObjectId.Empty)
            return Result.Fail(DomainErrors.Validation.InvalidFormat("SupplierId", "Valid ObjectId"));

        var oldItemName = ItemName;
        var oldUnit = Unit;
        var oldReorderLevel = ReorderLevel;
        var oldSupplierId = SupplierId;

        ItemName = itemName.Trim();
        Unit = unit.Trim();
        ReorderLevel = reorderLevel;
        SupplierId = supplierId;
        LastUpdated = DateTime.UtcNow;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new InventoryItemUpdatedEvent(Id, ShopId, oldItemName, ItemName, oldUnit, Unit, oldReorderLevel, ReorderLevel, oldSupplierId, SupplierId, updatedBy));
        return Result.Ok();
    }

    public Result AdjustQuantity(decimal quantityChange, string reason, ObjectId? adjustedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot adjust a deleted inventory item"));

        var newQuantity = Quantity + quantityChange;
        if (newQuantity < 0)
            return Result.Fail(DomainErrors.Business.InsufficientStock(ItemName, Quantity, Math.Abs(quantityChange)));

        var oldQuantity = Quantity;
        Quantity = newQuantity;
        LastUpdated = DateTime.UtcNow;
        SetAuditInfo(updatedBy: adjustedBy);
        AddDomainEvent(new InventoryQuantityAdjustedEvent(Id, ShopId, ItemName, oldQuantity, Quantity, quantityChange, reason, adjustedBy));
        return Result.Ok();
    }

    public Result SetQuantity(decimal quantity, ObjectId? updatedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Cannot set quantity of a deleted inventory item"));

        if (quantity < 0)
            return Result.Fail(DomainErrors.Validation.OutOfRange("Quantity", 0, "unlimited"));

        var oldQuantity = Quantity;
        Quantity = quantity;
        LastUpdated = DateTime.UtcNow;
        SetAuditInfo(updatedBy: updatedBy);
        AddDomainEvent(new InventoryQuantitySetEvent(Id, ShopId, ItemName, oldQuantity, Quantity, updatedBy));
        return Result.Ok();
    }

    public bool IsLowStock()
    {
        return Quantity <= ReorderLevel && ReorderLevel > 0;
    }

    public bool IsOutOfStock()
    {
        return Quantity <= 0;
    }

    public Result Deactivate(ObjectId? deletedBy = null)
    {
        if (IsDeleted)
            return Result.Fail(DomainErrors.Business.InvalidOperation("Inventory item is already deleted"));

        MarkAsDeleted(deletedBy);
        AddDomainEvent(new InventoryItemDeactivatedEvent(Id, ShopId, ItemName, deletedBy));
        return Result.Ok();
    }
}