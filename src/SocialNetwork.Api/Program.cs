using Scalar.AspNetCore;
using Serilog;
using SocialNetwork.Api.Middlewares;
using SocialNetwork.Api.OpenApi;
using SocialNetwork.Application;
using SocialNetwork.Infrastructure;

namespace SocialNetwork.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Логи пишемо в консоль (stdout) — у Docker і Azure App Service їх видно в log stream.
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
        app.MapHealthChecks("/health");

        app.Run();
    }
}
