using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Api.Services;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;
using CafeManagement.Infrastructure.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Diagnostics;

namespace CafeManagement.Infrastructure.Persistence;

public class ShopRepository : RepositoryBase<Shop>, IShopRepository
{
    private readonly IPerformanceMetricsService _metricsService;

    public ShopRepository(IMongoDbContext mongoDbContext, IPerformanceMetricsService metricsService) : base(mongoDbContext, "shops")
    {
        _metricsService = metricsService;

        // Register Bson class maps if not already registered
        if (!BsonClassMap.IsClassMapRegistered(typeof(Shop)))
        {
            RegisterShopClassMap();
        }
    }

    public async Task<Shop?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var filter = Builders<Shop>.Filter.And(
                Builders<Shop>.Filter.Eq(s => s.Name, name),
                Builders<Shop>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));

            var result = await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);

            stopwatch.Stop();
            _metricsService.RecordTiming("mongodb.shop.getbyname", stopwatch.Elapsed);
            _metricsService.RecordMetric("mongodb.query.count", 1, new Dictionary<string, string>
            {
                ["operation"] = "GetByName",
                ["collection"] = "shops"
            });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsService.RecordTiming("mongodb.shop.getbyname", stopwatch.Elapsed);
            _metricsService.RecordMetric("mongodb.query.error", 1, new Dictionary<string, string>
            {
                ["operation"] = "GetByName",
                ["collection"] = "shops",
                ["error"] = ex.GetType().Name
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<Shop>> GetByStatusAsync(ShopStatus status, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var filter = Builders<Shop>.Filter.And(
                Builders<Shop>.Filter.Eq(s => s.Status, status),
                Builders<Shop>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));

            var result = await Collection.Find(filter).ToListAsync(cancellationToken);

            stopwatch.Stop();
            _metricsService.RecordTiming("mongodb.shop.getbystatus", stopwatch.Elapsed);
            _metricsService.RecordMetric("mongodb.query.count", 1, new Dictionary<string, string>
            {
                ["operation"] = "GetByStatus",
                ["collection"] = "shops"
            });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsService.RecordTiming("mongodb.shop.getbystatus", stopwatch.Elapsed);
            _metricsService.RecordMetric("mongodb.query.error", 1, new Dictionary<string, string>
            {
                ["operation"] = "GetByStatus",
                ["collection"] = "shops",
                ["error"] = ex.GetType().Name
            });
            throw;
        }
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