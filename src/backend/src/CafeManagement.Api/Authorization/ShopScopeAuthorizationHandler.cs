using System.IO;
using System.Security.Claims;
using System.Text.Json;
using CafeManagement.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;

namespace CafeManagement.Api.Authorization;

/// <summary>
/// Authorization handler for shop-scoped access.
/// Validates that the user's shopId claim matches the requested resource's shopId.
/// Users with Admin or Manager roles are exempt from shop-scoped restrictions.
/// </summary>
public class ShopScopeAuthorizationHandler : AuthorizationHandler<ShopScopeRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ShopScopeAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ShopScopeRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // Check if user has an exempt role (Admin, Manager)
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (userRoles.Any(r => ShopScopeRequirement.ExemptRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }

        // Get user's shopId from claims
        var userShopIdClaim = context.User.FindFirst(ShopScopeRequirement.ShopIdClaimType);
        if (userShopIdClaim == null || !ObjectId.TryParse(userShopIdClaim.Value, out var userShopId))
        {
            // No shopId claim - cannot access shop-scoped resources
            return;
        }

        // Try to get shopId from route values (for REST endpoints like /api/shops/{shopId}/...)
        var routeShopId = GetShopIdFromRoute(httpContext);
        if (routeShopId.HasValue)
        {
            if (routeShopId.Value == userShopId)
            {
                context.Succeed(requirement);
            }
            return;
        }

        // Try to get shopId from query string (for queries like ?shopId=...)
        var queryShopId = GetShopIdFromQuery(httpContext);
        if (queryShopId.HasValue)
        {
            if (queryShopId.Value == userShopId)
            {
                context.Succeed(requirement);
            }
            return;
        }

        // Try to get shopId from request body (for POST/PUT endpoints with shopId in body)
        var bodyShopId = await GetShopIdFromBodyAsync(httpContext);
        if (bodyShopId.HasValue)
        {
            if (bodyShopId.Value == userShopId)
            {
                context.Succeed(requirement);
            }
            return;
        }

        // If no shopId in route, query, or body, deny by default
        return;
    }

    private static ObjectId? GetShopIdFromRoute(HttpContext httpContext)
    {
        var routeValues = httpContext.Request.RouteValues;
        if (routeValues.TryGetValue("shopId", out var shopIdValue) && shopIdValue is string shopIdString)
        {
            if (ObjectId.TryParse(shopIdString, out var shopId))
            {
                return shopId;
            }
        }
        return null;
    }

    private static ObjectId? GetShopIdFromQuery(HttpContext httpContext)
    {
        var query = httpContext.Request.Query;
        if (query.TryGetValue("shopId", out var shopIdValues) && ObjectId.TryParse(shopIdValues.FirstOrDefault(), out var shopId))
        {
            return shopId;
        }
        return null;
    }

    private static async Task<ObjectId?> GetShopIdFromBodyAsync(HttpContext httpContext)
    {
        var request = httpContext.Request;

        // Only read body for POST/PUT/PATCH methods
        if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method) && !HttpMethods.IsPatch(request.Method))
        {
            return null;
        }

        // Enable buffering so the body can be read multiple times
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("shopId", out var shopIdElement) &&
                shopIdElement.ValueKind == JsonValueKind.String &&
                ObjectId.TryParse(shopIdElement.GetString(), out var shopId))
            {
                return shopId;
            }
        }
        catch (JsonException)
        {
            // Invalid JSON - not our concern
        }

        return null;
    }
}