using CafeManagement.Domain.Common.Events;
using MongoDB.Bson;

namespace CafeManagement.Domain.Inventory.Events;

public record InventoryItemCreatedEvent(ObjectId Id, ObjectId ShopId, string ItemName, decimal Quantity, string Unit, ObjectId? CreatedBy) : DomainEvent;

public record InventoryItemUpdatedEvent(ObjectId Id, ObjectId ShopId, string OldItemName, string NewItemName, string OldUnit, string NewUnit, decimal OldReorderLevel, decimal NewReorderLevel, ObjectId? OldSupplierId, ObjectId? NewSupplierId, ObjectId? UpdatedBy) : DomainEvent;

public record InventoryQuantityAdjustedEvent(ObjectId Id, ObjectId ShopId, string ItemName, decimal OldQuantity, decimal NewQuantity, decimal QuantityChange, string Reason, ObjectId? AdjustedBy) : DomainEvent;

public record InventoryQuantitySetEvent(ObjectId Id, ObjectId ShopId, string ItemName, decimal OldQuantity, decimal NewQuantity, ObjectId? UpdatedBy) : DomainEvent;

public record InventoryItemDeactivatedEvent(ObjectId Id, ObjectId ShopId, string ItemName, ObjectId? DeletedBy) : DomainEvent;