using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Shops;
using CafeManagement.Infrastructure.Persistence;
using CafeManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB
        services.AddSingleton<IMongoDbContext>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("MongoDB");
            var databaseName = config.GetValue<string>("ConnectionStrings:DatabaseName") ?? "cafe_management";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            return new MongoDbContext(database);
        });

        // Repositories
        services.AddScoped<IShopRepository, ShopRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, MongoDbUnitOfWork>();

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}