using CafeManagement.Infrastructure.Identity;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Identity;

public class IdentityDbContext
{
    private readonly IMongoDbContext _mongoDbContext;

    public IdentityDbContext(IMongoDbContext mongoDbContext)
    {
        _mongoDbContext = mongoDbContext;
    }

    public IMongoCollection<ApplicationUser> Users => _mongoDbContext.GetCollection<ApplicationUser>("users");
    public IMongoCollection<ApplicationRole> Roles => _mongoDbContext.GetCollection<ApplicationRole>("roles");
    public IMongoCollection<IdentityUserClaim<ObjectId>> UserClaims => _mongoDbContext.GetCollection<IdentityUserClaim<ObjectId>>("userclaims");
    public IMongoCollection<IdentityUserLogin<ObjectId>> UserLogins => _mongoDbContext.GetCollection<IdentityUserLogin<ObjectId>>("userlogins");
    public IMongoCollection<IdentityRoleClaim<ObjectId>> RoleClaims => _mongoDbContext.GetCollection<IdentityRoleClaim<ObjectId>>("roleclaims");
    public IMongoCollection<IdentityUserToken<ObjectId>> UserTokens => _mongoDbContext.GetCollection<IdentityUserToken<ObjectId>>("usertokens");
}