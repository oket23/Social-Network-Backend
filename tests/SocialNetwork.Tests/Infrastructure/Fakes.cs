using System.Collections.Concurrent;
using System.Net;
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

    /// <summary>Останній лист на адресу з темою, що містить subjectPart, — повертає посилання з кнопки.</summary>
    public Uri GetLastLink(string toEmail, string subjectPart)
    {
        var email = _sent.Last(e => e.To == toEmail && e.Subject.Contains(subjectPart));
        var href = HrefRegex().Match(email.HtmlBody).Groups[1].Value;
        return new Uri(WebUtility.HtmlDecode(href));
    }

    [GeneratedRegex("href=\"([^\"]+)\"")]
    private static partial Regex HrefRegex();
}

public class FakeFileStorage : IFileStorage
{
    public Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken ct = default) =>
        Task.FromResult($"https://storage.test/media/{path}");

    public Task DeleteAsync(string url, CancellationToken ct = default) => Task.CompletedTask;
}
