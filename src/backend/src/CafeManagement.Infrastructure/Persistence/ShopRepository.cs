using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class ShopRepository : RepositoryBase<Shop>, IShopRepository
{
    public ShopRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "shops")
    {
        // Register Bson class maps if not already registered
        if (!BsonClassMap.IsClassMapRegistered(typeof(Shop)))
        {
            RegisterShopClassMap();
        }
    }

    public async Task<Shop?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Shop>.Filter.And(
            Builders<Shop>.Filter.Eq(s => s.Name, name),
            Builders<Shop>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));

        var result = await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<Shop>> GetByStatusAsync(ShopStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Shop>.Filter.And(
            Builders<Shop>.Filter.Eq(s => s.Status, status),
            Builders<Shop>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));

        var result = await Collection.Find(filter).ToListAsync(cancellationToken);

        return result;
    }

    private void RegisterShopClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Shop)))
            return;

        BsonClassMap.RegisterClassMap<Shop>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            // Map Id property
            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            // Map complex properties
            cm.MapMember(c => c.Address);
            cm.MapMember(c => c.Contact);
            cm.MapMember(c => c.OperatingHours);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}