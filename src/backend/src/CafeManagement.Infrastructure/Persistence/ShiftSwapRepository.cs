using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class ShiftSwapRepository : RepositoryBase<ShiftSwap>, IShiftSwapRepository
{
    public ShiftSwapRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "shiftSwaps")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(ShiftSwap)))
        {
            RegisterShiftSwapClassMap();
        }
    }

    public override async Task<ShiftSwap?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShiftSwap>.Filter.And(
            Builders<ShiftSwap>.Filter.Eq(s => s.Id, id),
            Builders<ShiftSwap>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftSwap>> GetByRequestingStaffIdAsync(ObjectId staffId, ShiftSwapStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<ShiftSwap>>
        {
            Builders<ShiftSwap>.Filter.Eq(s => s.RequestingStaffId, staffId),
            Builders<ShiftSwap>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (status.HasValue)
            filters.Add(Builders<ShiftSwap>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<ShiftSwap>.Filter.And(filters);
        return await Collection.Find(filter).SortByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftSwap>> GetByRequestedStaffIdAsync(ObjectId staffId, ShiftSwapStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<ShiftSwap>>
        {
            Builders<ShiftSwap>.Filter.Eq(s => s.RequestedStaffId, staffId),
            Builders<ShiftSwap>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (status.HasValue)
            filters.Add(Builders<ShiftSwap>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<ShiftSwap>.Filter.And(filters);
        return await Collection.Find(filter).SortByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftSwap>> GetByShopIdAsync(ObjectId shopId, ShiftSwapStatus? status = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<ShiftSwap>>
        {
            Builders<ShiftSwap>.Filter.Eq(s => s.ShopId, shopId),
            Builders<ShiftSwap>.Filter.Eq(s => s.DeletedAt, (DateTime?)null)
        };

        if (status.HasValue)
            filters.Add(Builders<ShiftSwap>.Filter.Eq(s => s.Status, status.Value));

        var filter = Builders<ShiftSwap>.Filter.And(filters);
        return await Collection.Find(filter).SortByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftSwap>> GetPendingByStaffAsync(ObjectId staffId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShiftSwap>.Filter.And(
            Builders<ShiftSwap>.Filter.Or(
                Builders<ShiftSwap>.Filter.Eq(s => s.RequestingStaffId, staffId),
                Builders<ShiftSwap>.Filter.Eq(s => s.RequestedStaffId, staffId)
            ),
            Builders<ShiftSwap>.Filter.Eq(s => s.DeletedAt, (DateTime?)null),
            Builders<ShiftSwap>.Filter.Eq(s => s.Status, ShiftSwapStatus.Pending)
        );
        return await Collection.Find(filter).SortByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftSwap>> GetPendingForApprovalAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ShiftSwap>.Filter.And(
            Builders<ShiftSwap>.Filter.Eq(s => s.ShopId, shopId),
            Builders<ShiftSwap>.Filter.Eq(s => s.DeletedAt, (DateTime?)null),
            Builders<ShiftSwap>.Filter.Eq(s => s.Status, ShiftSwapStatus.Accepted)
        );
        return await Collection.Find(filter).SortByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    private void RegisterShiftSwapClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(ShiftSwap)))
            return;

        BsonClassMap.RegisterClassMap<ShiftSwap>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.RequestingStaffId);
            cm.MapMember(c => c.RequestedStaffId);
            cm.MapMember(c => c.RequestingShiftId);
            cm.MapMember(c => c.RequestedShiftId);
            cm.MapMember(c => c.ShopId);
            cm.MapMember(c => c.Status);
            cm.MapMember(c => c.Reason);
            cm.MapMember(c => c.ApprovedBy);
            cm.MapMember(c => c.ApprovedAt);
            cm.MapMember(c => c.ApprovalNotes);
            cm.MapMember(c => c.RejectedBy);
            cm.MapMember(c => c.RejectedAt);
            cm.MapMember(c => c.RejectionReason);
            cm.MapMember(c => c.CancelledBy);
            cm.MapMember(c => c.CancelledAt);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}