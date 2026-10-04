using CafeManagement.Application.Common.Dtos;
using FluentResults;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResultDto>> RegisterAsync(string email, string password, string fullName, string? shopId = null);
    Task<Result<AuthResultDto>> LoginAsync(string email, string password);
    Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<Result> ForgotPasswordAsync(string email);
    Task<Result> ResetPasswordAsync(string email, string token, string newPassword);
    Task<Result> LogoutAsync(string refreshToken);
    Task<Result<UserDto>> GetCurrentUserAsync(string userId);
}