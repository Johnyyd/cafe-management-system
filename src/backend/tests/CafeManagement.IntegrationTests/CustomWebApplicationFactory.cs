using System;
using System.Collections.Generic;
using System.IO;
using CafeManagement.Api;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;
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
        Console.WriteLine("[CustomWebApplicationFactory] Constructor called");
        // Set test environment variables EARLY, before host builder is created
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_TEST", "true");
        Environment.SetEnvironmentVariable("DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS", "3600");
        _mongoDbContainer = mongoDbContainer;
        Console.WriteLine("[CustomWebApplicationFactory] Environment variables set in constructor");
        Console.WriteLine("[CustomWebApplicationFactory] Constructor complete");
    }

    public MongoDbContainer MongoDbContainer => _mongoDbContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var container = _mongoDbContainer;

        Console.WriteLine("[CustomWebApplicationFactory] ConfigureWebHost called");

        // Get connection string early to avoid any issues
        var connectionString = container.GetConnectionString();
        Console.WriteLine($"[CustomWebApplicationFactory] Connection string obtained: {connectionString}");

        // Completely override the configuration before the host is built
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration
            config.Sources.Clear();

            // Load test settings first
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
            if (File.Exists(testSettingsPath))
            {
                config.AddJsonFile(testSettingsPath, optional: false, reloadOnChange: false);
            }

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "your-super-secret-key-min-32-chars-change-in-production",
                ["Jwt:Issuer"] = "cafe-management",
                ["Jwt:Audience"] = "cafe-management-client",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
                ["ConnectionStrings:MongoDB"] = connectionString,
                ["ConnectionStrings:DatabaseName"] = "cafe_management_test",
                ["Seq:ServerUrl"] = "",
                // Completely override Serilog configuration - no Seq sink
                ["Serilog:MinimumLevel"] = "Information",
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["Serilog:WriteTo:0:Args:theme"] = "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
                ["Serilog:Using:0"] = "Serilog.Sinks.Console",
            });
        });

        // Override the host to use our Serilog configuration
        builder.ConfigureServices(services =>
        {
            Console.WriteLine("[CustomWebApplicationFactory] ConfigureServices called");
            // Remove the existing MongoDbContext registration
            var mongoDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMongoDbContext));
            if (mongoDescriptor != null)
            {
                services.Remove(mongoDescriptor);
                Console.WriteLine("[CustomWebApplicationFactory] Removed existing IMongoDbContext registration");
            }

            // Create MongoClient and IMongoDatabase
            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase("cafe_management_test");

            // Add MongoDB context
            services.AddSingleton<IMongoDbContext>(new MongoDbContext(mongoDatabase));
            Console.WriteLine("[CustomWebApplicationFactory] Added IMongoDbContext singleton");
        });

        builder.UseEnvironment("Development");
        Console.WriteLine("[CustomWebApplicationFactory] ConfigureWebHost complete");
    }

    protected override void Dispose(bool disposing)
    {
        // NEVER dispose - we keep the factory alive for the entire test run
        // base.Dispose(disposing);
    }
}