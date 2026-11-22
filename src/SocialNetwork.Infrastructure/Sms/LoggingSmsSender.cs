using Microsoft.Extensions.Logging;
using SocialNetwork.Application.Common.Abstractions;

namespace SocialNetwork.Infrastructure.Sms;

/// <summary>
/// Для локальної розробки без ключів Twilio: немає локального аналога smtp4dev для SMS,
/// тож просто логуємо повідомлення — код підтвердження видно в консолі застосунку.
/// </summary>
public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("SMS на {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
