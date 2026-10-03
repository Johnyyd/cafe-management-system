using CafeManagement.Application.Common.Interfaces;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class MongoDbUnitOfWork : IUnitOfWork
{
    private readonly IMongoDbContext _context;
    private readonly IClientSessionHandle _session;

    public MongoDbUnitOfWork(IMongoDbContext context)
    {
        _context = context;
        _session = context.Database.Client.StartSession();
        _session.StartTransaction();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _session.CommitTransactionAsync(cancellationToken);
        return 1;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}