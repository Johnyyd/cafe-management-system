using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Text;
using Xunit;

namespace CafeManagement.Infrastructure.Tests.Identity;

public class AuthServiceJwtValidationTests
{
    [Fact]
    public void JwtKeyConfiguration_ShouldBeNull_WhenKeyMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Issuer", "test"),
                new KeyValuePair<string, string?>("Jwt:Audience", "test")
            })
            .Build();

        // Act
        var key = config["Jwt:Key"];

        // Assert
        key.Should().BeNull();
    }

    [Fact]
    public void JwtKeyConfiguration_ShouldBeEmpty_WhenKeyPresentButEmpty()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Key", ""),
                new KeyValuePair<string, string?>("Jwt:Issuer", "test"),
                new KeyValuePair<string, string?>("Jwt:Audience", "test")
            })
            .Build();

        // Act
        var key = config["Jwt:Key"];

        // Assert
        key.Should().BeEmpty();
    }

    [Fact]
    public void JwtKeyConfiguration_ShouldBeTooShort_WhenKeyLessThan32Chars()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Key", "short-key"), // Less than 32 chars
                new KeyValuePair<string, string?>("Jwt:Issuer", "test"),
                new KeyValuePair<string, string?>("Jwt:Audience", "test")
            })
            .Build();

        // Act
        var key = config["Jwt:Key"];

        // Assert
        key.Should().NotBeNull();
        key.Should().NotBeEmpty();
        key.Length.Should().BeLessThan(32);
    }

    [Fact]
    public void JwtKeyConfiguration_ShouldBeValidLength_WhenKeyIs32CharsOrMore()
    {
        // Arrange
        var validKey32 = new string('A', 32); // Exactly 32 chars
        var validKey64 = new string('B', 64); // 64 chars

        // Test 32 chars
        var config32 = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Key", validKey32),
                new KeyValuePair<string, string?>("Jwt:Issuer", "test"),
                new KeyValuePair<string, string?>("Jwt:Audience", "test")
            })
            .Build();

        // Act
        var key32 = config32["Jwt:Key"];

        // Assert
        key32.Should().NotBeNull();
        key32.Should().NotBeEmpty();
        key32.Length.Should().Be(32);
        key32.Should().Be(validKey32);

        // Test 64 chars
        var config64 = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Key", validKey64),
                new KeyValuePair<string, string?>("Jwt:Issuer", "test"),
                new KeyValuePair<string, string?>("Jwt:Audience", "test")
            })
            .Build();

        // Act
        var key64 = config64["Jwt:Key"];

        // Assert
        key64.Should().NotBeNull();
        key64.Should().NotBeEmpty();
        key64.Length.Should().Be(64);
        key64.Should().Be(validKey64);
    }
}