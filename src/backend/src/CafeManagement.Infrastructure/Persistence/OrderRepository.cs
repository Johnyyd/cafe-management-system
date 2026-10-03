using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class OrderRepository : RepositoryBase<Domain.Orders.Order>, IOrderRepository
{
    public OrderRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "orders")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Domain.Orders.Order)))
        {
            RegisterOrderClassMap();
        }
    }

    public async Task<Domain.Orders.Order?> GetByIdWithItemsAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Orders.Order>.Filter.And(
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.Id, id),
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Orders.Order>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Orders.Order>.Filter.And(
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.ShopId, shopId),
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Orders.Order>> GetByShopIdAndStatusAsync(ObjectId shopId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Orders.Order>.Filter.And(
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.ShopId, shopId),
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.Status, status),
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Orders.Order>> GetByDateRangeAsync(ObjectId shopId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Domain.Orders.Order>.Filter.And(
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.ShopId, shopId),
            Builders<Domain.Orders.Order>.Filter.Gte(o => o.OrderTime, startDate),
            Builders<Domain.Orders.Order>.Filter.Lte(o => o.OrderTime, endDate),
            Builders<Domain.Orders.Order>.Filter.Eq(o => o.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    private void RegisterOrderClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Domain.Orders.Order)))
            return;

        BsonClassMap.RegisterClassMap<Domain.Orders.Order>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.ShopId);
            cm.MapMember(c => c.StaffId);
            cm.MapMember(c => c.CustomerInfo);
            cm.MapMember(c => c.Items);
            cm.MapMember(c => c.Status);
            cm.MapMember(c => c.PaymentStatus);
            cm.MapMember(c => c.TotalAmount);
            cm.MapMember(c => c.OrderTime);
            cm.MapMember(c => c.CompletedTime);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);

            // Map OrderItem nested class
            cm.MapMember(c => c.Items).SetSerializer(new ArraySerializer<OrderItem>());
        });
    }
}