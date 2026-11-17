using System.Security.Claims;
using CafeManagement.Application.Common.Authorization;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Inventory.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Bson;

namespace CafeManagement.Api.Authorization;

/// <summary>
/// Restricts inventory write operations to the caller's own shop.
/// Admin and Manager are exempt (they operate across shops). Every other role
/// must present a shopId claim that matches the shop owning the target item
/// (for routes with an {id}) or the shopId supplied on the command itself.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class InventoryShopScopeAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        var isExempt = user
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Any(r => ShopScopeRequirement.ExemptRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        if (isExempt)
        {
            await next();
            return;
        }

        var shopClaim = user.FindFirst(ShopScopeRequirement.ShopIdClaimType);
        if (shopClaim is null || !ObjectId.TryParse(shopClaim.Value, out var userShopId))
        {
            context.Result = Forbidden("Tài khoản chưa được gán chi nhánh làm việc.");
            return;
        }

        var targetShopId = ResolveTargetShopId(context);
        if (targetShopId is null && context.RouteData.Values.ContainsKey("id"))
        {
            // Route carried an item id: look the item up to learn its shop.
            if (ObjectId.TryParse(context.RouteData.Values["id"]?.ToString(), out var itemId))
            {
                var repository = context.HttpContext.RequestServices.GetRequiredService<IInventoryItemRepository>();
                var item = await repository.GetByIdAsync(itemId, context.HttpContext.RequestAborted);
                if (item is null)
                {
                    // Let the handler produce the usual 404.
                    await next();
                    return;
                }
                targetShopId = item.ShopId;
            }
        }

        if (targetShopId.HasValue && targetShopId.Value != userShopId)
        {
            context.Result = Forbidden("Bạn chỉ có thể thay đổi nguyên liệu thuộc chi nhánh của mình.");
            return;
        }

        await next();
    }

    private static ObjectId? ResolveTargetShopId(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is CreateInventoryItemCommand createCommand)
            {
                return createCommand.ShopId;
            }
        }

        return null;
    }

    private static ObjectResult Forbidden(string detail) => new(new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Forbidden",
        Detail = detail
    })
    {
        StatusCode = StatusCodes.Status403Forbidden
    };
}
