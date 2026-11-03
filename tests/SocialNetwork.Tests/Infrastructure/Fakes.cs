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

public class FakeFileStorage : IFileStorage
{
    public Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken ct = default) =>
        Task.FromResult($"https://storage.test/media/{path}");

    public Task DeleteAsync(string url, CancellationToken ct = default) => Task.CompletedTask;
}
