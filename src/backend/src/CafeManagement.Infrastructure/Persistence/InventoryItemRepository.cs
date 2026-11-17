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
        // Build filter: item must exist, not deleted, and resulting quantity >= 0
        // For negative quantityChange (consumption): Quantity + quantityChange >= 0 => Quantity >= -quantityChange
        // For positive quantityChange (restocking): always allowed (no upper bound check)
        var builder = Builders<Domain.Inventory.InventoryItem>.Filter;
        var filter = builder.And(
            builder.Eq(i => i.Id, id),
            builder.Eq(i => i.DeletedAt, (DateTime?)null));

        if (quantityChange < 0)
        {
            // Prevent negative stock: current Quantity must be >= absolute value of quantityChange
            filter = builder.And(filter, builder.Gte(i => i.Quantity, -quantityChange));
        }

        // Atomic update: increment Quantity, update LastUpdated and UpdatedBy
        var update = Builders<Domain.Inventory.InventoryItem>.Update
            .Inc(i => i.Quantity, quantityChange)
            .Set(i => i.LastUpdated, DateTime.UtcNow);

        if (adjustedBy.HasValue && adjustedBy.Value != ObjectId.Empty)
        {
            update = update.Set(i => i.UpdatedBy, adjustedBy.Value);
            update = update.Set(i => i.UpdatedAt, DateTime.UtcNow);
        }

        var options = new FindOneAndUpdateOptions<Domain.Inventory.InventoryItem>
        {
            ReturnDocument = ReturnDocument.After, // Return the updated document
            IsUpsert = false
        };

        var updatedItem = await Collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

        if (updatedItem == null)
        {
            // Check if item exists but would go negative
            var existsFilter = builder.And(
                builder.Eq(i => i.Id, id),
                builder.Eq(i => i.DeletedAt, (DateTime?)null));
            var exists = await Collection.Find(existsFilter).AnyAsync(cancellationToken);

            if (!exists)
                return Result.Fail(DomainErrors.General.NotFound("Inventory Item", id));

            // Item exists but would go negative
            var currentItem = await Collection.Find(existsFilter).SingleOrDefaultAsync(cancellationToken);
            return Result.Fail(DomainErrors.Business.InsufficientStock(currentItem?.ItemName ?? "Unknown", currentItem?.Quantity ?? 0, Math.Abs(quantityChange)));
        }

        return Result.Ok(updatedItem);
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