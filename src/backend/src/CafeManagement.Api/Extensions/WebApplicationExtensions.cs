using AspNetCoreRateLimit;
using CafeManagement.Api.Middleware;
using Serilog;
using System.Diagnostics;

namespace CafeManagement.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Health checks
        app.MapHealthChecks("/healthz");

        // Serilog request logging
        app.UseSerilogRequestLogging();

        // Security headers - Must be early in pipeline
        app.UseSecurityHeaders();

        // Correlation ID
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Performance monitoring
        app.UseMiddleware<PerformanceMonitoringMiddleware>();

        // Global error handling
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // HTTPS Redirection (disabled in development)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // CORS - Must be before Authentication/Authorization
        app.UseCors("CafeCorsPolicy");

        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cafe Management API v1");
            options.RoutePrefix = "swagger";
        });

        // Rate limiting
        app.UseIpRateLimiting();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        return app;
    }
}