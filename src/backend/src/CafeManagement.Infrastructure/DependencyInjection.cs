using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using CafeManagement.Domain.Menu;
using CafeManagement.Domain.Orders;
using CafeManagement.Infrastructure.Identity;
using CafeManagement.Infrastructure.Persistence;
using CafeManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text;

namespace CafeManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB
        services.AddSingleton<IMongoDbContext>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("MongoDB");
            var databaseName = config.GetValue<string>("ConnectionStrings:DatabaseName") ?? "cafe_management";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            return new MongoDbContext(database);
        });

        // Identity
        services.AddScoped<IUserStore<ApplicationUser>, UserStore>();
        services.AddScoped<IRoleStore<ApplicationRole>, RoleStore>();
        services.AddScoped<IdentityDbContext>();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddUserStore<UserStore>()
        .AddRoleStore<RoleStore>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtKey = configuration["Jwt:Key"];
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "cafe-management";
        var jwtAudience = configuration["Jwt:Audience"] ?? "cafe-management-client";

        // Only configure JWT if key is provided (allows tests to run without JWT)
        if (!string.IsNullOrEmpty(jwtKey))
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }

        services.AddAuthorization();

        // Repositories
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInventoryItemRepository, Infrastructure.Persistence.InventoryItemRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, MongoDbUnitOfWork>();

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IInventoryService, Infrastructure.Services.InventoryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        return services;
    }
}