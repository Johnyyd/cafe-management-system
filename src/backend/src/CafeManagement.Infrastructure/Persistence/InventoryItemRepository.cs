using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using CafeManagement.Domain.Inventory.Events;
using FluentResults;
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

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetLowStockItemsAsync(ObjectId? shopId = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Domain.Inventory.InventoryItem>>
        {
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null),
            Builders<Domain.Inventory.InventoryItem>.Filter.Gt(i => i.ReorderLevel, 0),
            new BsonDocument("$expr", new BsonDocument("$lte", new BsonArray { "$Quantity", "$ReorderLevel" }))
        };

        if (shopId.HasValue && shopId.Value != ObjectId.Empty)
            filters.Add(Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.ShopId, shopId.Value));

        return await Collection.Find(Builders<Domain.Inventory.InventoryItem>.Filter.And(filters)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetOutOfStockItemsAsync(ObjectId? shopId = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Domain.Inventory.InventoryItem>>
        {
            Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.DeletedAt, (DateTime?)null),
            Builders<Domain.Inventory.InventoryItem>.Filter.Lte(i => i.Quantity, 0)
        };

        if (shopId.HasValue && shopId.Value != ObjectId.Empty)
            filters.Add(Builders<Domain.Inventory.InventoryItem>.Filter.Eq(i => i.ShopId, shopId.Value));

        return await Collection.Find(Builders<Domain.Inventory.InventoryItem>.Filter.And(filters)).ToListAsync(cancellationToken);
    }

    public async Task<Result<Domain.Inventory.InventoryItem>> AdjustQuantityAtomicAsync(
        ObjectId id,
        decimal quantityChange,
        ObjectId? adjustedBy,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<Domain.Inventory.InventoryItem>.Filter;
        var itemFilter = builder.And(
            builder.Eq(i => i.Id, id),
            builder.Eq(i => i.DeletedAt, (DateTime?)null));

        // Quantities are persisted as strings (global decimal serializer), so MongoDB's
        // $inc cannot be used. Instead we do a guarded read-modify-write: the update only
        // applies while the quantity still equals the value we read, and we retry on a race.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var item = await Collection.Find(itemFilter).SingleOrDefaultAsync(cancellationToken);
            if (item is null)
            {
                return Result.Fail(DomainErrors.General.NotFound("Inventory Item", id));
            }

            var newQuantity = item.Quantity + quantityChange;
            if (newQuantity < 0)
            {
                return Result.Fail(DomainErrors.Business.InsufficientStock(
                    item.ItemName, item.Quantity, Math.Abs(quantityChange)));
            }

            var guardedFilter = builder.And(itemFilter, builder.Eq(i => i.Quantity, item.Quantity));
            var update = Builders<Domain.Inventory.InventoryItem>.Update
                .Set(i => i.Quantity, newQuantity)
                .Set(i => i.LastUpdated, DateTime.UtcNow);

            if (adjustedBy.HasValue && adjustedBy.Value != ObjectId.Empty)
            {
                update = update.Set(i => i.UpdatedBy, adjustedBy.Value);
                update = update.Set(i => i.UpdatedAt, DateTime.UtcNow);
            }

            var options = new FindOneAndUpdateOptions<Domain.Inventory.InventoryItem>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };

            var updatedItem = await Collection.FindOneAndUpdateAsync(guardedFilter, update, options, cancellationToken);
            if (updatedItem != null)
            {
                return Result.Ok(updatedItem);
            }

            // Someone else changed the quantity between our read and write - retry.
        }

        return Result.Fail("Inventory quantity was modified concurrently. Please retry.");
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