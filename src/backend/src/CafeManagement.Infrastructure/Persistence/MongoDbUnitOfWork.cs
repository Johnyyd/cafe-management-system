using CafeManagement.Application.Common.Interfaces;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class MongoDbUnitOfWork : IUnitOfWork
{
    private readonly IMongoDbContext _context;
    private readonly IClientSessionHandle? _session;
    private readonly bool _hasTransaction;

    public MongoDbUnitOfWork(IMongoDbContext context)
    {
        _context = context;
        try
        {
            _session = context.Database.Client.StartSession();
            _session.StartTransaction();
            _hasTransaction = true;
        }
        catch (NotSupportedException)
        {
            _hasTransaction = false;
        }
        catch (Exception)
        {
            _hasTransaction = false;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_hasTransaction && _session != null && _session.IsInTransaction)
        {
            await _session.CommitTransactionAsync(cancellationToken);
        }
        return 1;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}