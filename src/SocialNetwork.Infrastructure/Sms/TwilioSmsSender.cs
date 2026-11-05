using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using SocialNetwork.Application.Common.Abstractions;

namespace SocialNetwork.Infrastructure.Sms;

/// <summary>
/// Відправляє SMS через HTTP API Twilio (звичайний HTTPS-запит, без SDK Twilio).
/// Документація: https://www.twilio.com/docs/sms/send-messages
/// </summary>
public class TwilioSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly TwilioSettings _settings;

    public TwilioSmsSender(HttpClient httpClient, IOptions<TwilioSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["From"] = _settings.FromNumber,
                ["Body"] = message,
            }),
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Текст відповіді Twilio пояснює причину (невірний номер, недостатній баланс тощо) — потрапить у лог.
            var details = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Twilio API повернув {(int)response.StatusCode}: {details}", null, response.StatusCode);
        }
    }
}
