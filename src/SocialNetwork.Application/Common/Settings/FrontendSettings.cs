namespace SocialNetwork.Application.Common.Settings;

/// <summary>Адреса фронтенду — для посилань у листах (підтвердження email, скидання пароля).</summary>
public class FrontendSettings
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = "http://localhost:3000";
}
