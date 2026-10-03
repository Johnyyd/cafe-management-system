using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public abstract class RepositoryBase<T> : IRepository<T> where T : Entity<ObjectId>
{
    protected readonly IMongoCollection<T> Collection;
    protected readonly IMongoDbContext Context;

    protected RepositoryBase(IMongoDbContext context, string collectionName)
    {
        Context = context;
        Collection = context.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, id),
            Builders<T>.Filter.Eq(e => e.DeletedAt, (DateTime?)null));
        return await Collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.DeletedAt, (DateTime?)null);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public virtual async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        var update = Builders<T>.Update.Set(e => e.DeletedAt, DateTime.UtcNow);
        await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, id),
            Builders<T>.Filter.Eq(e => e.DeletedAt, (DateTime?)null));
        var count = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }
}