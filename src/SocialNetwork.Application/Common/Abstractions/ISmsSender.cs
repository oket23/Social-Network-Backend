namespace SocialNetwork.Application.Common.Abstractions;

/// <summary>
/// Відправка SMS. Реалізація — Twilio; локально, без ключів Twilio, SMS просто логуються.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
