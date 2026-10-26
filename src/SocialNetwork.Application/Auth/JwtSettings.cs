namespace SocialNetwork.Application.Auth;

/// <summary>
/// Налаштування JWT (секція "Jwt"). Key — секрет, зберігається в User Secrets / змінних середовища Render, не в репозиторії.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "SocialNetwork.Api";

    public string Audience { get; set; } = "SocialNetwork.Client";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
