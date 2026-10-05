using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CafeManagement.Api.Middleware;

/// <summary>
/// Middleware to add security headers to HTTP responses.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        context.Response.OnStarting(() =>
        {
            // Content Security Policy
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' https:; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            // X-Frame-Options
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // X-Content-Type-Options
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // X-XSS-Protection
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer-Policy
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions-Policy
            context.Response.Headers["Permissions-Policy"] =
                "accelerometer=(), " +
                "camera=(), " +
                "geolocation=(), " +
                "gyroscope=(), " +
                "magnetometer=(), " +
                "microphone=(), " +
                "payment=(), " +
                "usb=()";

            // X-Permitted-Cross-Domain-Policies
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

            // Cross-Origin-Opener-Policy
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Cross-Origin-Resource-Policy
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // Remove server header
            context.Response.Headers.Remove("Server");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension method to add SecurityHeadersMiddleware to the pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}