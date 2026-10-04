using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    ObjectId? ShopId = null
) : IRequest<Result<AuthResultDto>>;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResultDto>>;

public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<Result<AuthResultDto>>;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Result>;

public record ForgotPasswordCommand(
    string Email
) : IRequest<Result>;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest<Result>;

public record LogoutCommand(
    string RefreshToken
) : IRequest<Result>;
