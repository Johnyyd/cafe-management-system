using CafeManagement.Application.Auth.Commands;
using CafeManagement.Application.Auth.Queries;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CafeManagement.Application.Auth.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthResultDto>>
{
    private readonly IAuthService _authService;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(IAuthService authService, ILogger<RegisterHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result<AuthResultDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request.Email, request.Password, request.FullName, request.ShopId?.ToString());
        if (result.IsSuccess)
        {
            _logger.LogInformation("User registered: {Email}", request.Email);
        }
        return result;
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(IAuthService authService, ILogger<LoginHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged in: {Email}", request.Email);
        }
        else
        {
            _logger.LogWarning("Failed login attempt: {Email}", request.Email);
        }
        return result;
    }
}

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IAuthService _authService;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(IAuthService authService, ILogger<RefreshTokenHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refreshed successfully");
        }
        else
        {
            _logger.LogWarning("Token refresh failed");
        }
        return result;
    }
}

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(IAuthService authService, ICurrentUserService currentUserService, ILogger<ChangePasswordHandler> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Fail(DomainErrors.General.Unauthorized("User not authenticated"));

        var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Password changed for user: {UserId}", userId);
        }
        return result;
    }
}

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(IAuthService authService, ILogger<ForgotPasswordHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Password reset email sent to: {Email}", request.Email);
        }
        return result;
    }
}

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(IAuthService authService, ILogger<ResetPasswordHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Password reset for user: {Email}", request.Email);
        }
        return result;
    }
}

public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(IAuthService authService, ILogger<LogoutHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged out successfully");
        }
        return result;
    }
}

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetCurrentUserHandler> _logger;

    public GetCurrentUserHandler(IAuthService authService, ICurrentUserService currentUserService, ILogger<GetCurrentUserHandler> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Fail(DomainErrors.General.Unauthorized("User not authenticated"));

        return await _authService.GetCurrentUserAsync(userId);
    }
}