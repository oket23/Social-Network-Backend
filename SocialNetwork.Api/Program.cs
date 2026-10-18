using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SocialNetwork.Api.Data;
using SocialNetwork.Api.Models;
using SocialNetwork.Api.Services;

namespace SocialNetwork.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // TODO: замінити DevCurrentUserService на реалізацію на основі JWT, коли буде зроблено логін.
        builder.Services.AddScoped<ICurrentUserService, DevCurrentUserService>();
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}