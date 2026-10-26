using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SocialNetwork.Application.Common.Abstractions;

namespace SocialNetwork.Infrastructure.Email;

/// <summary>
/// Відправляє листи через HTTP API Brevo (звичайний HTTPS-запит), а не через SMTP.
/// Документація: https://developers.brevo.com/reference/sendtransacemail
/// </summary>
public class BrevoApiEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;

    public BrevoApiEmailSender(HttpClient httpClient, IOptions<BrevoSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = _settings.FromName, email = _settings.FromEmail },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent = htmlBody,
            }),
        };
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Headers.Add("accept", "application/json");

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Текст відповіді Brevo пояснює причину (невірний ключ, непідтверджений відправник тощо) — потрапить у лог.
            var details = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Brevo API повернув {(int)response.StatusCode}: {details}", null, response.StatusCode);
        }
    }
}
