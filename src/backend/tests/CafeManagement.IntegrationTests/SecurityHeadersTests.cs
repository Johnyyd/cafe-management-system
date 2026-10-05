using System.Net;
using System.Net.Http.Json;
using CafeManagement.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class SecurityHeadersTests
{
    private readonly HttpClient _client;

    public SecurityHeadersTests()
    {
        _client = IntegrationTestFixture.Client;
    }

    [Fact]
    public async Task Get_HealthEndpoint_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifySecurityHeaders(response);
    }

    [Fact]
    public async Task Get_ApiEndpoint_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/shops");

        // Assert - May return 401 (unauthorized) but should still have security headers
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        VerifySecurityHeaders(response);
    }

    [Fact]
    public async Task Post_ApiEndpoint_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/shops", new { name = "Test" });

        // Assert - May return 400/401 but should still have security headers
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.OK, HttpStatusCode.Created);
        VerifySecurityHeaders(response);
    }

    [Fact]
    public async Task Options_PreflightRequest_ShouldIncludeSecurityHeaders()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/shops");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        VerifySecurityHeaders(response);
    }

    [Fact]
    public async Task Get_SwaggerEndpoint_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Redirect);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            VerifySecurityHeaders(response);
        }
    }

    [Fact]
    public async Task Get_InvalidEndpoint_ShouldReturnNotFoundWithSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        VerifySecurityHeaders(response);
    }

    private static void VerifySecurityHeaders(HttpResponseMessage response)
    {
        // X-Content-Type-Options: nosniff
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        // X-Frame-Options: DENY or SAMEORIGIN
        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        var frameOptions = response.Headers.GetValues("X-Frame-Options").FirstOrDefault();
        frameOptions.Should().NotBeNullOrEmpty();
        frameOptions.Should().Match(x => x == "DENY" || x == "SAMEORIGIN");

        // X-XSS-Protection: 1; mode=block
        response.Headers.Contains("X-XSS-Protection").Should().BeTrue();
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");

        // Referrer-Policy: strict-origin-when-cross-origin or similar
        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.GetValues("Referrer-Policy").Should().NotBeNullOrEmpty();

        // Content-Security-Policy: should be present
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
        csp.Should().NotBeNullOrEmpty();

        // Permissions-Policy: should be present
        response.Headers.Contains("Permissions-Policy").Should().BeTrue();
        var permissions = response.Headers.GetValues("Permissions-Policy").FirstOrDefault();
        permissions.Should().NotBeNullOrEmpty();

        // Cross-Origin-Opener-Policy: same-origin
        response.Headers.Contains("Cross-Origin-Opener-Policy").Should().BeTrue();
        var coop = response.Headers.GetValues("Cross-Origin-Opener-Policy").FirstOrDefault();
        coop.Should().NotBeNullOrEmpty();

        // Cross-Origin-Resource-Policy: same-origin
        response.Headers.Contains("Cross-Origin-Resource-Policy").Should().BeTrue();
        var corp = response.Headers.GetValues("Cross-Origin-Resource-Policy").FirstOrDefault();
        corp.Should().NotBeNullOrEmpty();

        // Strict-Transport-Security (HSTS) - only in production, may not be in test
        // We'll check if it's present but won't fail if not in test environment
        // response.Headers.Contains("Strict-Transport-Security").Should().BeTrue();
    }
}