using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialNetwork.Infrastructure.Email;

namespace SocialNetwork.Tests.Unit;

public class BrevoApiEmailSenderTests
{
    private static readonly BrevoSettings Settings = new()
    {
        ApiKey = "test-api-key",
        FromEmail = "noreply@test.com",
        FromName = "Social Network",
    };

    [Fact]
    public async Task SendAsync_PostsCorrectRequestToBrevo()
    {
        var handler = new CapturingHandler(HttpStatusCode.Created);
        var sender = new BrevoApiEmailSender(new HttpClient(handler), Options.Create(Settings));

        await sender.SendAsync("user@test.com", "Тема", "<p>Привіт</p>");

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.Request.RequestUri!.ToString());
        Assert.Equal("test-api-key", handler.Request.Headers.GetValues("api-key").Single());

        using var json = JsonDocument.Parse(handler.Body!);
        var root = json.RootElement;
        Assert.Equal("noreply@test.com", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("user@test.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Тема", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Привіт</p>", root.GetProperty("htmlContent").GetString());
    }

    [Fact]
    public async Task SendAsync_BrevoError_ThrowsWithDetails()
    {
        var handler = new CapturingHandler(HttpStatusCode.Unauthorized, "{\"message\":\"Key not found\"}");
        var sender = new BrevoApiEmailSender(new HttpClient(handler), Options.Create(Settings));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sender.SendAsync("user@test.com", "Тема", "x"));

        Assert.Contains("401", ex.Message);
        Assert.Contains("Key not found", ex.Message);
    }

    private class CapturingHandler(HttpStatusCode status, string responseBody = "{}") : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
