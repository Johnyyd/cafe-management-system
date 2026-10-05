using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class TimeOffRequestRepository : RepositoryBase<TimeOffRequest>, ITimeOffRequestRepository
{
    public TimeOffRequestRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "timeOffRequests")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TimeOffRequest)))
        {
            RegisterTimeOffRequestClassMap();
        }
    }

    public override async Task<TimeOffRequest?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TimeOffRequest>.Filter.And(
            Builders<TimeOffRequest>.Filter.Eq(s => s.Id, id),
            Builders<TimeOffRequest>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeOffRequest>> GetByStaffIdAsync(ObjectId staffId, DateTime? startDate = null, DateTime? endDate = null, TimeOffStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<TimeOffRequest>>
        {
            Builders<TimeOffRequest>.Filter.Eq(s => s.StaffId, staffId),
            Builders<TimeOffRequest>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (startDate.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Gte(s => s.EndDate, startDate.Value));
        if (endDate.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Lte(s => s.StartDate, endDate.Value));
        if (status.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<TimeOffRequest>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeOffRequest>> GetByShopIdAsync(ObjectId shopId, DateTime? startDate = null, DateTime? endDate = null, TimeOffStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<TimeOffRequest>>
        {
            Builders<TimeOffRequest>.Filter.Eq(s => s.ShopId, shopId),
            Builders<TimeOffRequest>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (startDate.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Gte(s => s.EndDate, startDate.Value));
        if (endDate.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Lte(s => s.StartDate, endDate.Value));
        if (status.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<TimeOffRequest>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeOffRequest>> GetPendingByShopAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TimeOffRequest>.Filter.And(
            Builders<TimeOffRequest>.Filter.Eq(s => s.ShopId, shopId),
            Builders<TimeOffRequest>.Filter.Eq(s => s.DeletedAt, (DateTime?)null),
            Builders<TimeOffRequest>.Filter.Eq(s => s.Status, TimeOffStatus.Pending)
        );
        return await Collection.Find(filter).SortBy(s => s.StartDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeOffRequest>> GetActiveAsync(ObjectId? staffId = null, ObjectId? shopId = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var filters = new List<FilterDefinition<TimeOffRequest>>
        {
            Builders<TimeOffRequest>.Filter.Eq(s => s.DeletedAt, (DateTime?)null),
            Builders<TimeOffRequest>.Filter.Eq(s => s.Status, TimeOffStatus.Approved),
            Builders<TimeOffRequest>.Filter.Lte(s => s.StartDate, now),
            Builders<TimeOffRequest>.Filter.Gte(s => s.EndDate, now)
        };

        if (staffId.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Eq(s => s.StaffId, staffId.Value));
        if (shopId.HasValue)
            filters.Add(Builders<TimeOffRequest>.Filter.Eq(s => s.ShopId, shopId.Value));

        var filter = Builders<TimeOffRequest>.Filter.And(filters);
        return await Collection.Find(filter).SortBy(s => s.StartDate).ToListAsync(cancellationToken);
    }

    private void RegisterTimeOffRequestClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TimeOffRequest)))
            return;

        BsonClassMap.RegisterClassMap<TimeOffRequest>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.StaffId);
            cm.MapMember(c => c.ShopId);
            cm.MapMember(c => c.Type);
            cm.MapMember(c => c.StartDate);
            cm.MapMember(c => c.EndDate);
            cm.MapMember(c => c.Status);
            cm.MapMember(c => c.Reason);
            cm.MapMember(c => c.ApprovedBy);
            cm.MapMember(c => c.ApprovedAt);
            cm.MapMember(c => c.ApprovalNotes);
            cm.MapMember(c => c.RejectedBy);
            cm.MapMember(c => c.RejectedAt);
            cm.MapMember(c => c.RejectionReason);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}