using CafeManagement.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
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
            lock (_lock)
            {
                if (_factory == null)
                {
                    Console.WriteLine("[IntegrationTestFixture] Creating new CustomWebApplicationFactory");
                    _factory = new CustomWebApplicationFactory(_mongoDbContainer!);
                    Console.WriteLine("[IntegrationTestFixture] CustomWebApplicationFactory created");
                }
                return _factory;
            }
        }
    }

    public static HttpClient Client
    {
        get
        {
            lock (_lock)
            {
                if (_client == null)
                {
                    Console.WriteLine("[IntegrationTestFixture] Creating HttpClient from Factory.Server");
                    var server = Factory.Server;
                    Console.WriteLine("[IntegrationTestFixture] Factory.Server accessed");
                    _client = server.CreateClient();
                    _client.BaseAddress = new Uri("http://localhost");
                    Console.WriteLine("[IntegrationTestFixture] HttpClient created");
                }
                return _client;
            }
        }
    }

    public static MongoDbContainer MongoDbContainer => _mongoDbContainer!;

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

        // Create and start MongoDB container ONCE here, before factory creation
        Console.WriteLine("[IntegrationTestFixture] Creating MongoDB container");
        _mongoDbContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();
        _mongoDbContainer.StartAsync().GetAwaiter().GetResult();
        System.Threading.Tasks.Task.Delay(1000).GetAwaiter().GetResult(); // Wait for MongoDB to be ready
        Console.WriteLine("[IntegrationTestFixture] MongoDB container ready");

        // Force initialization of factory - this will trigger server build via EnsureServer
        _ = Factory;
        Console.WriteLine("[IntegrationTestFixture] Factory accessed in ModuleInitializer");
        // Force server build and client creation
        _ = Client;
        Console.WriteLine("[IntegrationTestFixture] Client accessed in ModuleInitializer");
        Console.WriteLine("[IntegrationTestFixture] ModuleInitializer complete");
    }
}