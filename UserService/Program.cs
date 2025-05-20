using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Handler;
using UserService.Repository;
using UserService.Service;

namespace UserService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDb");

        builder.Services.AddDbContext<UserDbContext>(options =>
        {
            if (useInMemory)
                options.UseInMemoryDatabase("TestDb");
            else
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserService, Service.UserService>();
        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();

        app.UseMiddleware<ExceptionHandlerMiddleware>();

        app.Run();
    }
}