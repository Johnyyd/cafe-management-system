using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CafeManagement.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace CafeManagement.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public MongoDbContainer MongoDbContainer { get; private set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing MongoDbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMongoDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add MongoDB container
            services.AddSingleton<IMongoDbContext>(sp =>
            {
                // Create MongoDB container if not already created
                MongoDbContainer ??= new MongoDbBuilder()
                    .WithImage("mongo:6.0")
                    .WithPortBinding(27017, true)
                    .Build();

                MongoDbContainer.Start();

                // Get the connection string
                var connectionString = MongoDbContainer.GetConnectionString();

                // Create MongoClient and IMongoDatabase
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase("cafe_management_test");

                // Return a MongoDbContext instance using the IMongoDatabase constructor
                return new MongoDbContext(mongoDatabase);
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        // Ensure MongoDB container is started
        if (MongoDbContainer == null)
        {
            MongoDbContainer = new MongoDbBuilder()
                .WithImage("mongo:6.0")
                .WithPortBinding(27017, true)
                .Build();

            MongoDbContainer.Start();
        }

        // Give MongoDB a moment to be ready
        await Task.Delay(1000);
    }
}