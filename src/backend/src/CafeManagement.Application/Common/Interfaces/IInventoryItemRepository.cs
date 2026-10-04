using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
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
    Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetOutOfStockItemsAsync(CancellationToken cancellationToken = default);
}