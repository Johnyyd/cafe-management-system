using CafeManagement.Domain.Inventory;
using MongoDB.Bson;

namespace CafeManagement.Application.Inventory.Responses;

public record InventoryItemResponse(
    ObjectId Id,
    ObjectId ShopId,
    string ItemName,
    string Unit,
    decimal Quantity,
    decimal ReorderLevel,
    ObjectId? SupplierId,
    bool IsLowStock,
    bool IsOutOfStock
);