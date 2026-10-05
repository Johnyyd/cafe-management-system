using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class ShiftRepository : RepositoryBase<Shift>, IShiftRepository
{
    public ShiftRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "shifts")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Shift)))
        {
            RegisterShiftClassMap();
        }
    }

    public override async Task<Shift?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Shift>.Filter.And(
            Builders<Shift>.Filter.Eq(s => s.Id, id),
            Builders<Shift>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shift>> GetByStaffIdAsync(ObjectId staffId, DateTime? startDate = null, DateTime? endDate = null, ShiftStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Shift>>
        {
            Builders<Shift>.Filter.Eq(s => s.StaffId, staffId),
            Builders<Shift>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (startDate.HasValue)
            filters.Add(Builders<Shift>.Filter.Gte(s => s.EndTime, startDate.Value));
        if (endDate.HasValue)
            filters.Add(Builders<Shift>.Filter.Lte(s => s.StartTime, endDate.Value));
        if (status.HasValue)
            filters.Add(Builders<Shift>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<Shift>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartTime).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shift>> GetByShopIdAsync(ObjectId shopId, DateTime? startDate = null, DateTime? endDate = null, ShiftStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Shift>>
        {
            Builders<Shift>.Filter.Eq(s => s.ShopId, shopId),
            Builders<Shift>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (startDate.HasValue)
            filters.Add(Builders<Shift>.Filter.Gte(s => s.EndTime, startDate.Value));
        if (endDate.HasValue)
            filters.Add(Builders<Shift>.Filter.Lte(s => s.StartTime, endDate.Value));
        if (status.HasValue)
            filters.Add(Builders<Shift>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<Shift>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartTime).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shift>> GetUpcomingAsync(ObjectId? staffId = null, ObjectId? shopId = null, int days = 7, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var endDate = now.AddDays(days);

        var filters = new List<FilterDefinition<Shift>>
        {
            Builders<Shift>.Filter.Eq(s => s.DeletedAt, (DateTime?)null),
            Builders<Shift>.Filter.Gte(s => s.EndTime, now),
            Builders<Shift>.Filter.Lte(s => s.StartTime, endDate),
            Builders<Shift>.Filter.Ne(s => s.Status, ShiftStatus.Cancelled)
        };

        if (staffId.HasValue)
            filters.Add(Builders<Shift>.Filter.Eq(s => s.StaffId, staffId.Value));
        if (shopId.HasValue)
            filters.Add(Builders<Shift>.Filter.Eq(s => s.ShopId, shopId.Value));

        var filter = Builders<Shift>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartTime).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shift>> GetNeedingCoverageAsync(ObjectId shopId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var filters = new List<FilterDefinition<Shift>>
        {
            Builders<Shift>.Filter.Eq(s => s.ShopId, shopId),
            Builders<Shift>.Filter.Eq(s => s.DeletedAt, (DateTime?)null),
            Builders<Shift>.Filter.Eq(s => s.Status, ShiftStatus.Scheduled),
            Builders<Shift>.Filter.Eq(s => s.CoveredByStaffId, (ObjectId?)null),
            Builders<Shift>.Filter.Gte(s => s.StartTime, now)
        };

        if (startDate.HasValue)
            filters.Add(Builders<Shift>.Filter.Gte(s => s.EndTime, startDate.Value));
        if (endDate.HasValue)
            filters.Add(Builders<Shift>.Filter.Lte(s => s.StartTime, endDate.Value));

        var filter = Builders<Shift>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartTime).ToListAsync(cancellationToken);
    }

    private void RegisterShiftClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Shift)))
            return;

        BsonClassMap.RegisterClassMap<Shift>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.StaffId);
            cm.MapMember(c => c.ShopId);
            cm.MapMember(c => c.StartTime);
            cm.MapMember(c => c.EndTime);
            cm.MapMember(c => c.Status);
            cm.MapMember(c => c.Notes);
            cm.MapMember(c => c.CoveredByStaffId);
            cm.MapMember(c => c.CoveredAt);
            cm.MapMember(c => c.CoveredBy);
            cm.MapMember(c => c.SwappedAt);
            cm.MapMember(c => c.SwappedWithStaffId);
            cm.MapMember(c => c.SwappedWithShiftId);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}