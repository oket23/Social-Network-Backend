using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialNetwork.Application.Auth;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Application.Common.Settings;
using SocialNetwork.Application.Users;
using SocialNetwork.Domain.Users;
using SocialNetwork.Infrastructure.Auth;
using SocialNetwork.Infrastructure.Email;
using SocialNetwork.Infrastructure.Persistence;
using SocialNetwork.Infrastructure.Persistence.Repositories;
using SocialNetwork.Infrastructure.Storage;

namespace SocialNetwork.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddSettings(services, configuration);
        AddPersistence(services, configuration);
        AddIdentity(services);

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IFileStorage, BlobFileStorage>();

        return services;
    }

    private static void AddSettings(IServiceCollection services, IConfiguration configuration)
    {
        // ValidateOnStart: якщо ключ JWT не задано, застосунок впаде одразу з зрозумілою помилкою,
        // а не на першому логіні.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => s.Key.Length >= 32,
                "Jwt:Key має містити щонайменше 32 символи. Задайте його через dotnet user-secrets (див. README).")
            .ValidateOnStart();

        services.Configure<FrontendSettings>(configuration.GetSection(FrontendSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure())
            // Таблиці й колонки в snake_case (users, display_name) — стандарт для PostgreSQL.
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }

    private static void AddIdentity(IServiceCollection services)
    {
        // AddIdentityCore — лише UserManager і сховище, без cookie-логіну (у нас JWT).
        services.AddIdentityCore<User>(options =>
            {
                // Мають збігатися з ValidationRules у Application.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_.";

                // Блокування після 5 невдалих спроб входу на 15 хвилин (захист від перебору паролів).
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<UkrainianIdentityErrorDescriber>();

        // Термін дії токенів підтвердження email і скидання пароля.
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(3));

        services.AddDataProtection()
            .SetApplicationName("SocialNetwork")
            .PersistKeysToDbContext<AppDbContext>();
    }
}
