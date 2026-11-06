using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SocialNetwork.Application.Common.Abstractions;

namespace SocialNetwork.Tests.Infrastructure;

public record SentEmail(string To, string Subject, string HtmlBody);

public partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _sent.Enqueue(new SentEmail(toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }

    public int CountSent(string toEmail, string subjectPart) =>
        _sent.Count(e => e.To == toEmail && e.Subject.Contains(subjectPart));

    /// <summary>Останній лист на адресу з темою, що містить subjectPart, — повертає 6-значний код з нього.</summary>
    public string GetLastCode(string toEmail, string subjectPart)
    {
        var email = _sent.Last(e => e.To == toEmail && e.Subject.Contains(subjectPart));
        return CodeRegex().Match(email.HtmlBody).Groups[1].Value;
    }

    [GeneratedRegex(">([0-9]{6})<")]
    private static partial Regex CodeRegex();
}

public record SentSms(string To, string Message);

public partial class FakeSmsSender : ISmsSender
{
    private readonly ConcurrentQueue<SentSms> _sent = new();

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _sent.Enqueue(new SentSms(phoneNumber, message));
        return Task.CompletedTask;
    }

    public int CountSent(string phoneNumber) => _sent.Count(s => s.To == phoneNumber);

    /// <summary>Остання SMS на номер — повертає 6-значний код з неї.</summary>
    public string GetLastCode(string phoneNumber)
    {
        var sms = _sent.Last(s => s.To == phoneNumber);
        return CodeRegex().Match(sms.Message).Groups[1].Value;
    }

    [GeneratedRegex("([0-9]{6})")]
    private static partial Regex CodeRegex();
}

public class FakeFileStorage : IFileStorage
{
    public Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken ct = default) =>
        Task.FromResult($"https://storage.test/media/{path}");

    public Task DeleteAsync(string url, CancellationToken ct = default) => Task.CompletedTask;
}
