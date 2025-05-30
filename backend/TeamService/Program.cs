using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeamService.Adapters.Inbound;
using TeamService.Adapters.Outbound.Clients;
using TeamService.Adapters.Outbound.Repositories;
using TeamService.Core.Ports;
using TeamService.Infrastructure.Data;

namespace TeamService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add controllers and Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Database configuration
        var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDb");
        builder.Services.AddDbContext<TeamDbContext>(options =>
        {
            if (useInMemory)
                options.UseInMemoryDatabase("TestDb");
            else
            {
                // Build connection string with environment variable support
                var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
                var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "ScrimsDb";
                var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
                var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
                
                var connectionString = $"Host={host};Database={database};Username={username};Password={password}";
                Console.WriteLine($"Connection string: {connectionString}");
                options.UseNpgsql(connectionString);
            }
        });

        // Dependency Injection
        builder.Services.AddScoped<ITeamRepository, TeamRepository>();
        builder.Services.AddScoped<ITeamService, Application.Services.TeamService>();
        builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client => {
            var userServiceBase = Environment.GetEnvironmentVariable("USERSERVICE_BASE") ?? 
                                  builder.Configuration["UserService:Base"] ?? 
                                  "http://localhost:5083";
            client.BaseAddress = new Uri(userServiceBase);
        });

        // JWT Authentication configuration
        ConfigureJwtAuthentication(builder);

        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Apply migrations at startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TeamDbContext>();
            db.Database.Migrate();
        }

        // Middleware pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static void ConfigureJwtAuthentication(WebApplicationBuilder builder)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                     builder.Configuration["Jwt:Key"] ?? 
                     "default-development-key-change-in-production";
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
                        builder.Configuration["Jwt:Issuer"] ?? 
                        "ScrimsApp";

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
    }
}