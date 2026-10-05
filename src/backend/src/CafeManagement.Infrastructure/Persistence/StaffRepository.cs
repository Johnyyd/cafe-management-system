using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class StaffRepository : RepositoryBase<Staff>, IStaffRepository
{
    public StaffRepository(IMongoDbContext mongoDbContext) : base(mongoDbContext, "staff")
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Staff)))
        {
            RegisterStaffClassMap();
        }
    }

    public async Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Staff>.Filter.And(
            Builders<Staff>.Filter.Eq(s => s.Contact.Email, email),
            Builders<Staff>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Staff>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Staff>.Filter.And(
            Builders<Staff>.Filter.ElemMatch(s => s.ShopAssignments, a => a.ShopId == shopId && a.IsActive),
            Builders<Staff>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Staff>> GetByRoleAsync(StaffRole role, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Staff>.Filter.And(
            Builders<Staff>.Filter.Eq(s => s.Role, role),
            Builders<Staff>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Staff>> GetByEmploymentStatusAsync(EmploymentStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Staff>.Filter.And(
            Builders<Staff>.Filter.Eq(s => s.EmploymentStatus, status),
            Builders<Staff>.Filter.Eq(s => s.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    private void RegisterStaffClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Staff)))
            return;

        BsonClassMap.RegisterClassMap<Staff>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            cm.MapIdMember(c => c.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId));

            cm.MapMember(c => c.FirstName);
            cm.MapMember(c => c.LastName);
            cm.MapMember(c => c.Role);
            cm.MapMember(c => c.Contact);
            cm.MapMember(c => c.EmploymentStatus);
            cm.MapMember(c => c.HireDate);
            cm.MapMember(c => c.ShopAssignments);
            cm.MapMember(c => c.CreatedAt);
            cm.MapMember(c => c.UpdatedAt);
            cm.MapMember(c => c.DeletedAt);
            cm.MapMember(c => c.CreatedBy);
            cm.MapMember(c => c.UpdatedBy);
        });
    }
}