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
}