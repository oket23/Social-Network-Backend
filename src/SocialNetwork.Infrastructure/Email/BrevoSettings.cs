namespace SocialNetwork.Infrastructure.Email;

/// <summary>
/// Секція "Brevo" — відправка листів через HTTP API Brevo (порт 443).
/// Використовується там, де SMTP-порти заблоковані (Render free блокує 25/465/587).
/// Якщо ApiKey порожній — застосунок відправляє листи через SMTP (локально — smtp4dev).
/// </summary>
public class BrevoSettings
{
    public const string SectionName = "Brevo";

    /// <summary>API key з Brevo → SMTP &amp; API → API keys (не плутати з SMTP key).</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = "https://api.brevo.com/v3/smtp/email";

    /// <summary>Підтверджений у Brevo відправник.</summary>
    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Social Network";
}
