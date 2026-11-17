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

    protected IClientSessionHandle? Session => Context.CurrentSession;

    private IFindFluent<T, T> FindWithSession(FilterDefinition<T> filter)
    {
        return Session != null
            ? Collection.Find(Session, filter)
            : Collection.Find(filter);
    }

    public virtual async Task<T?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, id),
            Builders<T>.Filter.Eq(e => e.DeletedAt, (DateTime?)null));
        return await FindWithSession(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.DeletedAt, (DateTime?)null);
        return await FindWithSession(filter).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (Session != null)
        {
            await Collection.InsertOneAsync(Session, entity, cancellationToken: cancellationToken);
        }
        else
        {
            await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        if (Session != null)
        {
            await Collection.ReplaceOneAsync(Session, filter, entity, cancellationToken: cancellationToken);
        }
        else
        {
            await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }
    }

    public virtual async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        var update = Builders<T>.Update.Set(e => e.DeletedAt, DateTime.UtcNow);
        if (Session != null)
        {
            await Collection.UpdateOneAsync(Session, filter, update, cancellationToken: cancellationToken);
        }
        else
        {
            await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
    }

    public virtual async Task<bool> ExistsAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, id),
            Builders<T>.Filter.Eq(e => e.DeletedAt, (DateTime?)null));
        var count = Session != null
            ? await Collection.CountDocumentsAsync(Session, filter, cancellationToken: cancellationToken)
            : await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }
}