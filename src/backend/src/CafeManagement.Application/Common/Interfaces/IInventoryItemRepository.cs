using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IInventoryItemRepository : IRepository<Domain.Inventory.InventoryItem>
{
    Task<Domain.Inventory.InventoryItem?> GetByShopIdAndNameAsync(ObjectId shopId, string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetItemNamesAsync(ObjectId shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetLowStockItemsAsync(ObjectId? shopId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetOutOfStockItemsAsync(ObjectId? shopId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts quantity with optimistic concurrency (guarded read-modify-write) and a
    /// filter preventing negative stock. Returns the updated item on success, or a
    /// failure when the item is missing or the adjustment would go below zero.
    /// </summary>
    Task<Result<Domain.Inventory.InventoryItem>> AdjustQuantityAtomicAsync(
        ObjectId id,
        decimal quantityChange,
        ObjectId? adjustedBy,
        CancellationToken cancellationToken = default);
}