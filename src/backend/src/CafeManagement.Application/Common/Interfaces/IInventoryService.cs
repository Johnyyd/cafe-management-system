using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IInventoryService
{
    Task<Result<InventoryItem>> CreateAsync(ObjectId shopId, string itemName, string unit, decimal quantity, decimal reorderLevel, ObjectId? supplierId = null, ObjectId? createdBy = null);
    Task<Result<InventoryItem>> UpdateAsync(ObjectId id, string itemName, string unit, decimal reorderLevel, ObjectId? supplierId, ObjectId? updatedBy = null);
    Task<Result> AdjustQuantityAsync(ObjectId id, decimal quantityChange, string reason, ObjectId? adjustedBy = null);
    Task<Result> SetQuantityAsync(ObjectId id, decimal quantity, ObjectId? updatedBy = null);
    Task<Result> DeactivateAsync(ObjectId id, ObjectId? deletedBy = null);
    Task<IReadOnlyList<InventoryItem>> GetLowStockItemsAsync();
    Task<IReadOnlyList<InventoryItem>> GetOutOfStockItemsAsync();
    Task<IReadOnlyList<string>> GetItemNamesAsync(ObjectId shopId);
    Task<Result<InventoryItem>> GetByShopIdAndNameAsync(ObjectId shopId, string name);
}