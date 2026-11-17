using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;
    private IClientSessionHandle? _currentSession;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB");
        var databaseName = configuration.GetValue<string>("ConnectionStrings:DatabaseName") ?? "cafe_management";

        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);
    }

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
        _client = database.Client;
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    public IClientSessionHandle? CurrentSession => _currentSession;

    public async Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default)
    {
        _currentSession = await _client.StartSessionAsync(cancellationToken: cancellationToken);
        return _currentSession;
    }

    public async Task StartTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSession == null)
        {
            await StartSessionAsync(cancellationToken);
        }
        _currentSession!.StartTransaction();
        await Task.CompletedTask;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSession != null && _currentSession.IsInTransaction)
        {
            try
            {
                await _currentSession.CommitTransactionAsync(cancellationToken);
            }
            finally
            {
                _currentSession.Dispose();
                _currentSession = null;
            }
        }
    }

    public async Task AbortTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSession != null && _currentSession.IsInTransaction)
        {
            try
            {
                await _currentSession.AbortTransactionAsync(cancellationToken);
            }
            finally
            {
                _currentSession.Dispose();
                _currentSession = null;
            }
        }
    }
}