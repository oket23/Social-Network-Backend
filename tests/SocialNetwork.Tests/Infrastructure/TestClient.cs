using System.Net.Http.Headers;
using System.Net.Http.Json;
using SocialNetwork.Application.Auth.Responses;

namespace SocialNetwork.Tests.Infrastructure;

/// <summary>Формат відповіді API для десеріалізації в тестах.</summary>
public class Envelope<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }
}

public static class TestClient
{
    public const string Password = "Passw0rd!";

    public static object NewRegistration(string? password = null)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        return new
        {
            email = $"user_{id}@test.com",
            username = $"user_{id}",
            displayName = "Test User",
            password = password ?? Password,
        };
    }

    public static async Task<AuthResponse> Register(this HttpClient client, object? registration = null)
    {
        var response = await client.PostAsJsonAsync("/auth/register", registration ?? NewRegistration());
        response.EnsureSuccessStatusCode();
        return (await response.ReadEnvelope<AuthResponse>()).Data!;
    }

    public static HttpClient Authorize(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task<Envelope<T>> ReadEnvelope<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Envelope<T>>())!;
}
