using CafeManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Security.Claims;

namespace CafeManagement.Infrastructure.Identity;

public class UserStore :
    IUserStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>,
    IUserRoleStore<ApplicationUser>,
    IUserClaimStore<ApplicationUser>,
    IUserLoginStore<ApplicationUser>,
    IQueryableUserStore<ApplicationUser>
{
    private readonly IdentityDbContext _context;
    private readonly IMongoCollection<ApplicationUser> _users;

    public UserStore(IdentityDbContext context)
    {
        _context = context;
        _users = context.Users;
    }

    public IQueryable<ApplicationUser> Users => _users.AsQueryable();

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await _users.InsertOneAsync(user, cancellationToken: cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == user.Id, cancellationToken);
        return result.DeletedCount > 0 ? IdentityResult.Success : IdentityResult.Failed();
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var objectId))
            return null;
        return await _users.Find(u => u.Id == objectId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await _users.Find(u => u.NormalizedUserName == normalizedUserName).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0 ? IdentityResult.Success : IdentityResult.Failed();
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await _users.Find(u => u.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        if (!user.Roles.Contains(roleName))
        {
            user.Roles.Add(roleName);
            await UpdateAsync(user, cancellationToken);
        }
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        user.Roles.Remove(roleName);
        await UpdateAsync(user, cancellationToken);
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        => user.Roles.ToList();

    public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        => user.Roles.Contains(roleName);

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        return await _users.Find(u => u.Roles.Contains(roleName)).ToListAsync(cancellationToken);
    }

    public async Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken)
        => user.Claims.Select(c => new Claim(c.ClaimType ?? string.Empty, c.ClaimValue ?? string.Empty)).ToList();

    public async Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            user.Claims.Add(new IdentityUserClaim<ObjectId>
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            });
        }
        await UpdateAsync(user, cancellationToken);
    }

    public async Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var existingClaim = user.Claims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        if (existingClaim != null)
        {
            existingClaim.ClaimType = newClaim.Type;
            existingClaim.ClaimValue = newClaim.Value;
            await UpdateAsync(user, cancellationToken);
        }
    }

    public async Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var existingClaim = user.Claims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
            if (existingClaim != null)
            {
                user.Claims.Remove(existingClaim);
            }
        }
        await UpdateAsync(user, cancellationToken);
    }

    public async Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        return await _users.Find(u => u.Claims.Any(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value)).ToListAsync(cancellationToken);
    }

    public async Task AddLoginAsync(ApplicationUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        user.Logins.Add(new IdentityUserLogin<ObjectId>
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName
        });
        await UpdateAsync(user, cancellationToken);
    }

    public async Task RemoveLoginAsync(ApplicationUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var login = user.Logins.FirstOrDefault(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        if (login != null)
        {
            user.Logins.Remove(login);
            await UpdateAsync(user, cancellationToken);
        }
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user, CancellationToken cancellationToken)
        => user.Logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList();

    public async Task<ApplicationUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        return await _users.Find(u => u.Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey)).FirstOrDefaultAsync(cancellationToken);
    }

    public void Dispose() { }
}

public class RoleStore :
    IRoleStore<ApplicationRole>,
    IQueryableRoleStore<ApplicationRole>
{
    private readonly IdentityDbContext _context;
    private readonly IMongoCollection<ApplicationRole> _roles;

    public RoleStore(IdentityDbContext context)
    {
        _context = context;
        _roles = context.Roles;
    }

    public IQueryable<ApplicationRole> Roles => _roles.AsQueryable();

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        await _roles.InsertOneAsync(role, cancellationToken: cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var result = await _roles.DeleteOneAsync(r => r.Id == role.Id, cancellationToken);
        return result.DeletedCount > 0 ? IdentityResult.Success : IdentityResult.Failed();
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(roleId, out var objectId))
            return null;
        return await _roles.Find(r => r.Id == objectId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        return await _roles.Find(r => r.NormalizedName == normalizedRoleName).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.NormalizedName);

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Id.ToString());

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Name);

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var result = await _roles.ReplaceOneAsync(r => r.Id == role.Id, role, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0 ? IdentityResult.Success : IdentityResult.Failed();
    }

    public void Dispose() { }
}