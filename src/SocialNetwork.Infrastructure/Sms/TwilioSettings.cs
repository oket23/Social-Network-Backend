namespace SocialNetwork.Infrastructure.Sms;

/// <summary>
/// Секція "Twilio" — відправка SMS через HTTP API Twilio.
/// Якщо AccountSid/AuthToken порожні — SMS просто логуються (локальна розробка).
/// </summary>
public class TwilioSettings
{
    public const string SectionName = "Twilio";

    public string AccountSid { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Номер відправника з Twilio (E.164), з якого приходять SMS.</summary>
    public string FromNumber { get; set; } = string.Empty;
}
