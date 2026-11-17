using CafeManagement.Application.Auth.Commands;
using CafeManagement.Application.Auth.Queries;
using CafeManagement.Application.Common.Dtos;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        // Set refresh token as HttpOnly cookie
        SetRefreshTokenCookie(result.Value.RefreshToken, IsDevelopment);

        // Return auth result without refresh token in body
        var response = new AuthResultDto(
            result.Value.AccessToken,
            string.Empty, // Don't send refresh token in body
            result.Value.ExpiresIn,
            result.Value.TokenType,
            result.Value.User
        );

        return Created(string.Empty, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return Unauthorized(result.Errors);

        // Set refresh token as HttpOnly cookie
        SetRefreshTokenCookie(result.Value.RefreshToken, IsDevelopment);

        // Return auth result without refresh token in body
        var response = new AuthResultDto(
            result.Value.AccessToken,
            string.Empty, // Don't send refresh token in body
            result.Value.ExpiresIn,
            result.Value.TokenType,
            result.Value.User
        );

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        // Read refresh token from HttpOnly cookie
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new[] { "Refresh token not found" });

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return Unauthorized(result.Errors);

        // Set new refresh token as HttpOnly cookie
        SetRefreshTokenCookie(result.Value.RefreshToken, IsDevelopment);

        // Return auth result without refresh token in body
        var response = new AuthResultDto(
            result.Value.AccessToken,
            string.Empty,
            result.Value.ExpiresIn,
            result.Value.TokenType,
            result.Value.User
        );

        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Read refresh token from HttpOnly cookie
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var command = new LogoutCommand(refreshToken);
            var result = await _mediator.Send(command, cancellationToken);
            // Ignore result - we want to clear cookie regardless
        }

        // Clear the refresh token cookie
        ClearRefreshTokenCookie(IsDevelopment);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery();
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return Unauthorized(result.Errors);

        return Ok(result.Value);
    }

    private void SetRefreshTokenCookie(string refreshToken, bool isDevelopment = false)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment, // Require HTTPS only in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7), // Match refresh token expiry
            Path = "/api" // Changed from "/api/v1/auth" to cover all API versions (migration: clients may need cookie clear)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie(bool isDevelopment = false)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1), // Expire in the past
            Path = "/api"
        };
        Response.Cookies.Append("refreshToken", string.Empty, cookieOptions);
    }

    private bool IsDevelopment => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
}