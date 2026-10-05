using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CafeManagement.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace CafeManagement.IntegrationTests;

/// <summary>
/// Tests for async refactoring to verify no deadlocks or race conditions
/// These tests run concurrent operations to verify thread safety
/// </summary>
public class AsyncRefactoringTests
{
    private readonly HttpClient _client;

    public AsyncRefactoringTests()
    {
        _client = IntegrationTestFixture.Client;
    }

    [Fact]
    public async Task Concurrent_HealthCheck_Requests_ShouldAllSucceed()
    {
        // Arrange
        const int concurrentRequests = 10;
        var tasks = new Task<HttpResponseMessage>[concurrentRequests];

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks[i] = _client.GetAsync("/healthz");
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Contains("Content-Type").Should().BeTrue();
            response.Headers.GetValues("Content-Type").Should().Contain("application/json");
        }
    }

    [Fact]
    public async Task Concurrent_Unauthorized_Api_Requests_ShouldAllReturnSameStatus()
    {
        // Arrange
        const int concurrentRequests = 10;
        var tasks = new Task<HttpResponseMessage>[concurrentRequests];

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks[i] = _client.GetAsync("/api/v1/shops");
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Mixed_Concurrent_Requests_ShouldNotDeadlock()
    {
        // Act - Mix of different endpoint requests
        var tasks = new Task<HttpResponseMessage>[]
        {
            _client.GetAsync("/healthz"),
            _client.GetAsync("/api/v1/shops"),
            _client.GetAsync("/swagger/v1/swagger.json"),
            _client.GetAsync("/healthz"),
            _client.GetAsync("/api/v1/shops")
        };

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert - All should complete without hanging
        responses.Length.Should().Be(5);
        responses[0].StatusCode.Should().Be(HttpStatusCode.OK); // healthz
        responses[1].StatusCode.Should().Be(HttpStatusCode.Unauthorized); // shops
        responses[2].StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Redirect); // swagger
        responses[3].StatusCode.Should().Be(HttpStatusCode.OK); // healthz
        responses[4].StatusCode.Should().Be(HttpStatusCode.Unauthorized); // shops
    }

    [Fact]
    public async Task Sequential_Versus_Concurrent_Performance_ShouldBeReasonable()
    {
        // Arrange
        const int requestCount = 5;

        // Sequential timing
        var sequentialStart = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < requestCount; i++)
        {
            await _client.GetAsync("/healthz");
        }
        sequentialStart.Stop();

        // Concurrent timing
        var concurrentTasks = new Task[requestCount];
        for (int i = 0; i < requestCount; i++)
        {
            concurrentTasks[i] = _client.GetAsync("/healthz");
        }
        var concurrentStart = System.Diagnostics.Stopwatch.StartNew();
        await Task.WhenAll(concurrentTasks);
        concurrentStart.Stop();

        // Assert
        // Concurrent should be faster than or equal to sequential (allowing for variance)
        // This test mainly ensures we don't deadlock - if we did, concurrent would hang
        concurrentStart.ElapsedMilliseconds.Should().BeLessThan(sequentialStart.ElapsedMilliseconds * 2);
    }
}