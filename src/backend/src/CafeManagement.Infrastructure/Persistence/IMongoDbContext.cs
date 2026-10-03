using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Persistence;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<T> GetCollection<T>(string name);
}