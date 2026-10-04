using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Identity;

namespace CafeManagement.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<ObjectId>
{
    public string FullName { get; set; } = string.Empty;

    [BsonElement("shopId")]
    public ObjectId? ShopId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("refreshTokens")]
    public List<RefreshToken> RefreshTokens { get; set; } = new();

    // Simple string-based roles and claims (not using Identity's built-in navigation properties)
    [BsonElement("roles")]
    public List<string> Roles { get; set; } = new();

    [BsonElement("claims")]
    public List<IdentityUserClaim<ObjectId>> Claims { get; set; } = new();

    [BsonElement("logins")]
    public List<IdentityUserLogin<ObjectId>> Logins { get; set; } = new();

    [BsonElement("tokens")]
    public List<IdentityUserToken<ObjectId>> Tokens { get; set; } = new();
}

public class RefreshToken
{
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("expires")]
    public DateTime Expires { get; set; }

    [BsonElement("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [BsonElement("revoked")]
    public DateTime? Revoked { get; set; }

    [BsonElement("revokedByIp")]
    public string? RevokedByIp { get; set; }

    public bool IsActive => Revoked == null && DateTime.UtcNow < Expires;
}

public class ApplicationRole : IdentityRole<ObjectId>
{
    public string Description { get; set; } = string.Empty;
}