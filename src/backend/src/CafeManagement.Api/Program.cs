using CafeManagement.Api.Extensions;
using Serilog;

namespace CafeManagement.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Log the timeout environment variable for debugging
            var timeoutVar = Environment.GetEnvironmentVariable("DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS");
            Console.WriteLine($"[Program.Main] DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS = {timeoutVar}");

            // Check if we're running in test mode (no Seq sink)
            var isTest = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TEST") == "true";
            Console.WriteLine($"[Program.Main] IsTest: {isTest}");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Console.WriteLine("[Program.Main] Creating WebApplicationBuilder");
                var builder = WebApplication.CreateBuilder(args);
                Console.WriteLine("[Program.Main] WebApplicationBuilder created");

                // Override Serilog for tests to disable Seq
                Console.WriteLine("[Program.Main] Configuring Serilog");
                if (isTest)
                {
                    builder.Host.UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.Console());
                    Console.WriteLine("[Program.Main] Serilog configured for test mode");
                }
                else
                {
                    builder.Host.UseSerilog((context, services, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext());
                    Console.WriteLine("[Program.Main] Serilog configured for non-test mode");
                }

                Console.WriteLine("[Program.Main] Adding application services");
                builder.Services.AddApplicationServices(builder.Configuration);
                Console.WriteLine("[Program.Main] Application services added");

                Console.WriteLine("[Program.Main] Adding infrastructure services");
                builder.Services.AddInfrastructureServices(builder.Configuration);
                Console.WriteLine("[Program.Main] Infrastructure services added");

                Console.WriteLine("[Program.Main] Adding API services");
                builder.Services.AddApiServices(builder.Configuration);
                Console.WriteLine("[Program.Main] API services added");

                Console.WriteLine("[Program.Main] Building application");
                var app = builder.Build();
                Console.WriteLine("[Program.Main] Application built");

                Console.WriteLine("[Program.Main] Using API pipeline");
                app.UseApiPipeline();
                Console.WriteLine("[Program.Main] API pipeline used");

                // Don't call app.Run() in test mode - let WebApplicationFactory handle the server
                if (!isTest)
                {
                    Console.WriteLine("[Program.Main] Running application (non-test mode)");
                    app.Run();
                }
                else
                {
                    Console.WriteLine("[Program.Main] Test mode detected, not calling app.Run()");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program.Main] Exception occurred: {ex}");
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Console.WriteLine("[Program.Main] In finally block");
                Log.CloseAndFlush();
            }
        }
    }
}