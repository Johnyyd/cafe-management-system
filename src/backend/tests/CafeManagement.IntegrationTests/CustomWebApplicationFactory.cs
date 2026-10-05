using System;
using System.Collections.Generic;
using CafeManagement.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.MongoDb;

namespace CafeManagement.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests with Testcontainers MongoDB
/// This is a singleton that never gets disposed
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MongoDbContainer _mongoDbContainer;

    public CustomWebApplicationFactory(MongoDbContainer mongoDbContainer)
    {
        _mongoDbContainer = mongoDbContainer ?? throw new ArgumentNullException(nameof(mongoDbContainer));
    }

    protected override Microsoft.Extensions.Hosting.IHostBuilder CreateHostBuilder()
    {
        // Set test environment variables EARLY, before host builder is used
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_TEST", "true");
        Environment.SetEnvironmentVariable("DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS", "14400");

        var builder = base.CreateHostBuilder();

        var connectionString = _mongoDbContainer.GetConnectionString();
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:MongoDB", connectionString),
                new KeyValuePair<string, string?>("ConnectionStrings:DatabaseName", "cafe_management_test"),
                new KeyValuePair<string, string?>("Serilog:MinimumLevel", "Information"),
                new KeyValuePair<string, string?>("Serilog:WriteTo:0:Name", "Console"),
            });
        });

        return builder;
    }
}