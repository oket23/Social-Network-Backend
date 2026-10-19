namespace SocialNetwork.Application.Common.Abstractions;

/// <summary>
/// Відправка листів. Реалізація — SMTP (MailKit): локально smtp4dev, у проді Brevo/Gmail.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
