using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeamService.Client;
using TeamService.Data;
using TeamService.Repository;
using TeamService.Service;

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
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        // Dependency Injection
        builder.Services.AddScoped<ITeamRepository, TeamRepository>();
        builder.Services.AddScoped<ITeamService, Service.TeamService>();
        builder.Services.AddHttpClient<UserServiceClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5083"); 
        });

        // JWT Authentication configuration
        ConfigureJwtAuthentication(builder);

        builder.Services.AddAuthorization();

        var app = builder.Build();

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
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "sua-chave-secreta-bem-grande";
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SeuIssuer";

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