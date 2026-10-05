using CafeManagement.Api.Extensions;
using Serilog;

namespace CafeManagement.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Check if we're running in test mode (no Seq sink)
            var isTest = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TEST") == "true";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Override Serilog for tests to disable Seq
                if (isTest)
                {
                    builder.Host.UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.Console());
                }
                else
                {
                    builder.Host.UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext());
                }

                builder.Services.AddApplicationServices(builder.Configuration);
                builder.Services.AddInfrastructureServices(builder.Configuration);
                builder.Services.AddApiServices(builder.Configuration);

                var app = builder.Build();

                app.UseApiPipeline();

                // Don't call app.Run() in test mode - let WebApplicationFactory handle the server
                if (!isTest)
                {
                    app.Run();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}