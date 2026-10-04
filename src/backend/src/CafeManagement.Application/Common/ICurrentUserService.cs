using System.Security.Claims;

namespace CafeManagement.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? ShopId { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
    ClaimsPrincipal? User { get; }
}