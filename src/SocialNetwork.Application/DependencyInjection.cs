using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SocialNetwork.Application.Auth;
using SocialNetwork.Application.Users;

namespace SocialNetwork.Application;

public static class DependencyInjection
{
    /// <summary>Реєструє сервіси бізнес-логіки та валідатори. Новий модуль — новий рядок тут.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }
}
