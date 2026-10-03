using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Menu;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class MenuItemRepository : RepositoryBase<Domain.Menu.MenuItem>, IMenuItemRepository
{
    public MenuItemRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "menuItems")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Domain.Menu.MenuItem)))
        {
            RegisterMenuItemClassMap();
        }
    }

    public async Task<Domain.Menu.MenuItem?> GetByShopIdAndNameAsync(ObjectId shopId, string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Menu.MenuItem>.Filter.And(
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.ShopId, shopId),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.Name, name),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Menu.MenuItem>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Menu.MenuItem>.Filter.And(
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.ShopId, shopId),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Menu.MenuItem>> GetByCategoryAsync(ObjectId shopId, string category, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Menu.MenuItem>.Filter.And(
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.ShopId, shopId),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.Category, category),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Menu.MenuItem>> GetByStatusAsync(ObjectId shopId, MenuItemStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Menu.MenuItem>.Filter.And(
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.ShopId, shopId),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.Status, status),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Menu.MenuItem>.Filter.And(
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.ShopId, shopId),
            Builders<Domain.Menu.MenuItem>.Filter.Eq(m => m.DeletedAt, (DateTime?)null));
        var categories = await Collection.DistinctAsync<string>("Category", filter, cancellationToken: cancellationToken);
        return await categories.ToListAsync(cancellationToken);
    }

    private void RegisterMenuItemClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Domain.Menu.MenuItem)))
            return;

        BsonClassMap.RegisterClassMap<Domain.Menu.MenuItem>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.ShopId);
            cm.MapMember(c => c.Category);
            cm.MapMember(c => c.Name);
            cm.MapMember(c => c.Description);
            cm.MapMember(c => c.Price);
            cm.MapMember(c => c.Ingredients);
            cm.MapMember(c => c.Allergens);
            cm.MapMember(c => c.Availability);
            cm.MapMember(c => c.Status);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}