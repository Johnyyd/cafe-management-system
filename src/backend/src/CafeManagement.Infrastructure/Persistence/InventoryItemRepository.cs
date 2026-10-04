using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using CafeManagement.Domain.Inventory.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class InventoryItemRepository : RepositoryBase<Domain.Inventory.InventoryItem>, IInventoryItemRepository
{
    public InventoryItemRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "inventoryItems")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Domain.Inventory.InventoryItem)))
        {
            RegisterInventoryItemClassMap();
        }
    }

    public async Task<Domain.Inventory.InventoryItem?> GetByShopIdAndNameAsync(ObjectId shopId, string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Inventory.InventoryItem>.Filter.And(
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.ShopId, shopId),
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.ItemName, name),
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Inventory.InventoryItem>.Filter.And(
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.ShopId, shopId),
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetItemNamesAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Inventory.InventoryItem>.Filter.And(
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.ShopId, shopId),
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null));
        var itemNames = await Collection.DistinctAsync<string>("ItemName", filter, cancellationToken: cancellationToken);
        return await itemNames.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Inventory.InventoryItem>.Filter.And(
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null),
            Builders<Domain.Inventory.InventoryItem>.Filter.Lte("Quantity", "$ReorderLevel"));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetOutOfStockItemsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Inventory.InventoryItem>.Filter.And(
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null),
            Builders<Domain.Inventory.InventoryItem>.Filter.Lte(i => i.Quantity, 0));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    private void RegisterInventoryItemClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Domain.Inventory.InventoryItem)))
            return;

        BsonClassMap.RegisterClassMap<Domain.Inventory.InventoryItem>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.ShopId);
            cm.MapMember(c => c.ItemName);
            cm.MapMember(c => c.Unit);
            cm.MapMember(c => c.Quantity);
            cm.MapMember(c => c.ReorderLevel);
            cm.MapMember(c => c.SupplierId);
            cm.MapMember(c => c.LastUpdated);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}