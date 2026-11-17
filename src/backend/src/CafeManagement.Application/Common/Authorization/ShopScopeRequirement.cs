using Microsoft.AspNetCore.Authorization;

namespace CafeManagement.Application.Common.Authorization;

/// <summary>
/// Authorization requirement for shop-scoped access.
/// Ensures the user can only access resources belonging to their assigned shop.
/// </summary>
public class ShopScopeRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The claim type that holds the user's shop ID.
    /// </summary>
    public const string ShopIdClaimType = "shopId";

    /// <summary>
    /// Roles that are exempt from shop-scoped authorization (can access all shops).
    /// </summary>
    public static readonly string[] ExemptRoles = ["Admin", "Manager"];
}