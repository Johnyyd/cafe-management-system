using System.Net;
using System.Net.Http.Headers;
using CafeManagement.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class CorsPolicyTests
{
    private readonly HttpClient _client;

    public CorsPolicyTests()
    {
        _client = IntegrationTestFixture.Client;
    }

    [Fact]
    public async Task Get_WithAllowedOrigin_ShouldIncludeCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/shops");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:3000");
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
        response.Headers.Contains("Access-Control-Allow-Headers").Should().BeTrue();
    }

    [Fact]
    public async Task Get_WithDisallowedOrigin_ShouldNotIncludeCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/shops");
        request.Headers.Add("Origin", "http://evil.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        // Note: Some browsers/frameworks may still return the header but with different value
        // The key is that it should not be the evil origin
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            response.Headers.GetValues("Access-Control-Allow-Origin")
                .Should().NotContain("http://evil.example.com");
        }
    }

    [Fact]
    public async Task Post_WithAllowedOrigin_ShouldIncludeCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shops");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:5173");
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
        response.Headers.Contains("Access-Control-Allow-Headers").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Contain("Content-Type");
    }

    [Fact]
    public async Task Options_PreflightRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/shops");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type,Authorization");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:3000");
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Contain("POST");
        response.Headers.Contains("Access-Control-Allow-Headers").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Contain("Content-Type");
        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Contain("Authorization");
    }

    [Fact]
    public async Task Get_WithoutOrigin_ShouldNotIncludeCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/shops");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // When no Origin header is present, CORS headers should not be added
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}