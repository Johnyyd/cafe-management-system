using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<T> GetCollection<T>(string name);
    IClientSessionHandle? CurrentSession { get; }
    Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default);
    Task StartTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task AbortTransactionAsync(CancellationToken cancellationToken = default);
}