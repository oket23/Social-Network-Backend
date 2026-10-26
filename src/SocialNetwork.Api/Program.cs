using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using SocialNetwork.Api.Middlewares;
using SocialNetwork.Api.OpenApi;
using SocialNetwork.Application;
using SocialNetwork.Infrastructure;
using SocialNetwork.Infrastructure.Persistence;

namespace SocialNetwork.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Логи пишемо в консоль (stdout) — у Docker і на Render їх видно у вкладці Logs.
        builder.Services.AddSerilog((services, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        // Кожен шар реєструє свої залежності сам (див. DependencyInjection.cs у кожному проєкті).
        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration)
            .AddApi(builder.Configuration);

        var app = builder.Build();

        // Локально (Development) нові міграції застосовуються автоматично при старті —
        // не треба після кожного git pull запускати "dotnet ef database update".
        // На staging/production міграції застосовує CI (deploy-staging.yml), а не застосунок.
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        }

        app.UseForwardedHeaders();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseSerilogRequestLogging();

        // Документація API: /openapi/v1.json і UI на /scalar. На staging теж увімкнено (OpenApi:Enabled).
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options
                .WithTitle("Social Network API")
                .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName));
        }

        app.UseHttpsRedirection();

        app.UseCors(DependencyInjection.FrontendCorsPolicy);
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        // /health — API живий І база доступна (для деплою та діагностики).
        app.MapHealthChecks("/health");

        // /health/live — лише "процес працює", без звернення до БД. Його пінгує UptimeRobot,
        // щоб Render не засинав, а база Neon при цьому могла засинати й не витрачала безкоштовний ліміт.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

        app.Run();
    }
}
