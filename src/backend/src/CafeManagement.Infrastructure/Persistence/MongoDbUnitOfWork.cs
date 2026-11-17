using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Infrastructure.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation using MongoDB transactions.
/// Wraps IMongoDbContext to provide transaction management without exposing infrastructure details to Application layer.
/// </summary>
public class MongoDbUnitOfWork : IUnitOfWork
{
    private readonly IMongoDbContext _mongoDbContext;
    private bool _disposed;

    public MongoDbUnitOfWork(IMongoDbContext mongoDbContext)
    {
        _mongoDbContext = mongoDbContext;
    }

    public async Task StartTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _mongoDbContext.StartTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _mongoDbContext.CommitTransactionAsync(cancellationToken);
    }

    public async Task AbortTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _mongoDbContext.AbortTransactionAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Note: We don't dispose the MongoDbContext here as it's managed by DI container
            _disposed = true;
        }
    }
}