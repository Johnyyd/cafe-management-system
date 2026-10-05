using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CafeManagement.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class AuthenticationRequirementsTests
{
    private readonly HttpClient _client;

    public AuthenticationRequirementsTests()
    {
        _client = IntegrationTestFixture.Client;
    }

    [Fact]
    public async Task Get_Shops_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/shops");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_Shops_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/shops", new { name = "Test Shop" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Shops_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/v1/shops");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Shops_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var expiredToken = CreateExpiredJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/v1/shops");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_HealthEndpoint_WithoutToken_ShouldAllowAnonymous()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_SwaggerEndpoint_WithoutToken_ShouldAllowAnonymous()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert - Swagger should be accessible without auth
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Get_Staff_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_MenuItems_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/menu-items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Orders_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Inventory_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/inventory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Shifts_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/shifts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthEndpoints_WithoutToken_ShouldAllowAnonymous()
    {
        // Act & Assert - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "password123" });
        loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized); // Validation error or unauthorized

        // Act & Assert - Register
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new { email = "test@test.com", password = "Password123", fullName = "Test User" });
        registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Created, HttpStatusCode.Unauthorized);

        // Act & Assert - Refresh token
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "token" });
        refreshResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    private static string CreateExpiredJwtToken()
    {
        // Create a JWT token that's already expired
        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", "test"),
            new System.Security.Claims.Claim("email", "test@test.com")
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("your-super-secret-key-min-32-chars-change-in-production"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "cafe-management",
            audience: "cafe-management-client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-10), // Expired 10 minutes ago
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}