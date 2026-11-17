using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Infrastructure.Identity;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CafeManagement.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMongoCollection<ApplicationUser> _usersCollection;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        ICurrentUserService currentUserService,
        IdentityDbContext identityDbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _currentUserService = currentUserService;
        _usersCollection = identityDbContext.Users;
    }

    public async Task<Result<AuthResultDto>> RegisterAsync(string email, string password, string fullName, string? shopId = null, string? role = null)
    {
        // Self-service callers never get to choose their role or shop. Only an
        // authenticated Admin may provision elevated roles and shop assignments
        // (used by admin tooling and the seed script).
        var isAdmin = _currentUserService.IsAuthenticated
            && _currentUserService.Roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));

        var requestedRole = isAdmin && CanonicalRoleNames.TryGetValue(role ?? string.Empty, out var canonicalRole)
            ? canonicalRole
            : "Barista";

        ObjectId? requestedShopId = null;
        if (isAdmin && !string.IsNullOrWhiteSpace(shopId) && ObjectId.TryParse(shopId, out var parsedShopId))
        {
            requestedShopId = parsedShopId;
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (!isAdmin)
            {
                return Result.Fail("Email already registered");
            }

            // Admin provisioning is idempotent: re-sync the role and shop on an existing account.
            var updated = false;
            if (existingUser.ShopId != requestedShopId)
            {
                existingUser.ShopId = requestedShopId;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(fullName) && existingUser.FullName != fullName)
            {
                existingUser.FullName = fullName;
                updated = true;
            }
            if (updated)
            {
                await _userManager.UpdateAsync(existingUser);
            }

            await ApplyRoleAsync(existingUser, requestedRole);
            return await GenerateAuthResultAsync(existingUser);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            ShopId = requestedShopId
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return Result.Fail("Registration failed: Invalid email or password");
        }

        await ApplyRoleAsync(user, requestedRole);

        return await GenerateAuthResultAsync(user);
    }

    private async Task ApplyRoleAsync(ApplicationUser user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = role, NormalizedName = role.ToUpperInvariant() });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var staleRoles = currentRoles
            .Where(r => !string.Equals(r, role, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (staleRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, staleRoles);
        }
        if (!currentRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)))
        {
            await _userManager.AddToRoleAsync(user, role);
        }
    }


    public async Task<Result<AuthResultDto>> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Fail("Invalid credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Result.Fail("Invalid credentials");
        }

        return await GenerateAuthResultAsync(user);
    }

    public async Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken)
    {
        var user = await FindUserByRefreshTokenAsync(refreshToken);

        if (user == null)
        {
            return Result.Fail("Invalid refresh token");
        }

        var token = user.RefreshTokens.First(rt => rt.Token == refreshToken);
        if (!token.IsActive)
        {
            return Result.Fail("Refresh token expired or revoked");
        }

        // Revoke the used refresh token
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = _currentUserService.User?.FindFirst("ip")?.Value ?? "unknown";

        // Generate new tokens
        var newAccessToken = await GenerateJwtTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();
        user.RefreshTokens.Add(newRefreshToken);

        await _userManager.UpdateAsync(user);

        return await CreateAuthResultAsync(newAccessToken, newRefreshToken.Token, user);
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        if (!ObjectId.TryParse(userId, out var objectId))
        {
            return Result.Fail("Invalid user ID");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Fail("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Fail($"Password change failed: {errors}");
        }

        // Changing the password invalidates every existing refresh token.
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            token.Revoked = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result.Ok();
    }

    public async Task<Result> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal if user exists
            return Result.Ok();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // In production, send email with token
        _logger.LogInformation("Password reset token generated for {Email}", email);

        return Result.Ok();
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Fail("Invalid reset token");
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Fail($"Password reset failed: {errors}");
        }

        // A reset invalidates every existing refresh token.
        foreach (var existing in user.RefreshTokens.Where(t => t.IsActive))
            existing.Revoked = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result.Ok();
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        var user = await FindUserByRefreshTokenAsync(refreshToken);

        if (user != null)
        {
            var token = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
            if (token != null)
            {
                token.Revoked = DateTime.UtcNow;
                token.RevokedByIp = _currentUserService.User?.FindFirst("ip")?.Value ?? "unknown";
                await _userManager.UpdateAsync(user);
            }
        }

        return Result.Ok();
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(string userId)
    {
        if (!ObjectId.TryParse(userId, out var objectId))
        {
            return Result.Fail("Invalid user ID");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Fail("User not found");
        }

        return Result.Ok(await MapToUserDtoAsync(user));
    }

    private async Task<ApplicationUser?> FindUserByRefreshTokenAsync(string refreshToken)
    {
        var filter = Builders<ApplicationUser>.Filter.ElemMatch(
            u => u.RefreshTokens,
            rt => rt.Token == refreshToken
        );
        return await _usersCollection.Find(filter).FirstOrDefaultAsync();
    }

    private async Task<Result<AuthResultDto>> GenerateAuthResultAsync(ApplicationUser user)
    {
        var accessToken = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();
        user.RefreshTokens.Add(refreshToken);
        await _userManager.UpdateAsync(user);

        return await CreateAuthResultAsync(accessToken, refreshToken.Token, user);
    }

    private async Task<AuthResultDto> CreateAuthResultAsync(string accessToken, string refreshToken, ApplicationUser user)
    {
        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: GetAccessTokenExpiryMinutes() * 60,
            TokenType: "Bearer",
            User: await MapToUserDtoAsync(user)
        );
    }

    private static readonly Dictionary<string, string> CanonicalRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = "Admin",
        ["Manager"] = "Manager",
        ["Cashier"] = "Cashier",
        ["Barista"] = "Barista",
        ["InventoryStaff"] = "InventoryStaff"
    };

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT Key is not configured. Set JWT_KEY environment variable or Jwt:Key in configuration.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("fullName", user.FullName),
            new("shopId", user.ShopId?.ToString() ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            // UserManager normalizes stored role names to upper case; authorization
            // compares role claims case-sensitively against "Admin", "Manager", ...
            var canonicalRole = CanonicalRoleNames.TryGetValue(role, out var known)
                ? known
                : role;
            claims.Add(new Claim(ClaimTypes.Role, canonicalRole));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "cafe-management",
            audience: _configuration["Jwt:Audience"] ?? "cafe-management-client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            Expires = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            Created = DateTime.UtcNow
        };
    }

    private int GetAccessTokenExpiryMinutes()
        => int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var minutes) ? minutes : 15;

    private int GetRefreshTokenExpiryDays()
        => int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;

    private async Task<UserDto> MapToUserDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: user.FullName,
            ShopId: user.ShopId,
            Roles: roles
        );
    }
}