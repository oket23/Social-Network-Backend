namespace SocialNetwork.Infrastructure.Email;

/// <summary>
/// Секція "Smtp". Локально — smtp4dev (localhost:2525, без логіна, Security=None),
/// у проді — Brevo (smtp-relay.brevo.com:587, StartTls) або Gmail (smtp.gmail.com:587, app password).
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 2525;

    /// <summary>None | Auto | StartTls | SslOnConnect.</summary>
    public string Security { get; set; } = "None";

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromEmail { get; set; } = "no-reply@socialnetwork.local";

    public string FromName { get; set; } = "Social Network";
}
