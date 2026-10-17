using CafeManagement.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using System.Threading;
using Testcontainers.MongoDb;

namespace CafeManagement.IntegrationTests;

/// <summary>
/// Singleton that provides a shared WebApplicationFactory for all integration tests
/// Initializes once per test assembly using ModuleInitializer
/// </summary>
public class IntegrationTestFixture
{
    // Static constructor to set environment variable as early as possible
    static IntegrationTestFixture()
    {
        Console.WriteLine("[IntegrationTestFixture] Static constructor setting timeout");
        Environment.SetEnvironmentVariable("DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS", "14400"); // 4 hours
    }

    private static CustomWebApplicationFactory? _factory;
    private static HttpClient? _client;
    private static MongoDbContainer? _mongoDbContainer;
    private static readonly object _lock = new();

    public static CustomWebApplicationFactory Factory
    {
        get
        {
            if (_factory == null)
            {
                lock (_lock)
                {
                    if (_factory == null)
                    {
                        var factoryTask = InitializeFactoryAsync();
                        factoryTask.Wait(); // Safe in static context
                        _factory = factoryTask.Result;
                    }
                }
            }
            return _factory!;
        }
    }

    public static HttpClient Client
    {
        get
        {
            if (_client == null)
            {
                lock (_lock)
                {
                    if (_client == null)
                    {
                        var clientTask = InitializeClientAsync();
                        clientTask.Wait(); // Safe in static context
                        _client = clientTask.Result;
                    }
                }
            }
            return _client!;
        }
    }

    public static MongoDbContainer MongoDbContainer
    {
        get
        {
            if (_mongoDbContainer == null)
            {
                lock (_lock)
                {
                    if (_mongoDbContainer == null)
                    {
                        var containerTask = InitializeMongoDbAsync();
                        containerTask.Wait(); // Safe in static context
                        _mongoDbContainer = containerTask.Result;
                    }
                }
            }
            return _mongoDbContainer!;
        }
    }

    private static async Task<CustomWebApplicationFactory> InitializeFactoryAsync()
    {
        Console.WriteLine("[IntegrationTestFixture] Creating new CustomWebApplicationFactory");
        var mongoDbContainer = await InitializeMongoDbAsync();
        var factory = new CustomWebApplicationFactory(mongoDbContainer);
        Console.WriteLine("[IntegrationTestFixture] CustomWebApplicationFactory created");
        return factory;
    }

    private static async Task<HttpClient> InitializeClientAsync()
    {
        Console.WriteLine("[IntegrationTestFixture] Creating HttpClient from Factory.Server");
        var factory = Factory;
        var server = factory.Server;
        Console.WriteLine("[IntegrationTestFixture] Factory.Server accessed");
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost");
        Console.WriteLine("[IntegrationTestFixture] HttpClient created");
        return client;
    }

    private static async Task<MongoDbContainer> InitializeMongoDbAsync()
    {
        Console.WriteLine("[IntegrationTestFixture] Creating MongoDB container");
        var container = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();
        await container.StartAsync();
        await System.Threading.Tasks.Task.Delay(1000); // Wait for MongoDB to be ready
        Console.WriteLine("[IntegrationTestFixture] MongoDB container ready");
        return container;
    }

    /// <summary>
    /// Module initializer - runs once when the assembly loads
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        Console.WriteLine("[IntegrationTestFixture] ModuleInitializer starting");

        // Verify timeout is still set
        string timeoutVar = Environment.GetEnvironmentVariable("DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS") ?? "not set";
        Console.WriteLine($"[IntegrationTestFixture] DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS = {timeoutVar}");

        // Force initialization - accessing the properties will trigger lazy initialization
        _ = Factory;
        _ = Client;
        _ = MongoDbContainer;

        Console.WriteLine("[IntegrationTestFixture] Factory accessed in ModuleInitializer");
        Console.WriteLine("[IntegrationTestFixture] Client accessed in ModuleInitializer");
        Console.WriteLine("[IntegrationTestFixture] ModuleInitializer complete");
    }
}