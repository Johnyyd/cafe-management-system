using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB");
        var databaseName = configuration.GetValue<string>("ConnectionStrings:DatabaseName") ?? "cafe_management";

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
}