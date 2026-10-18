using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SocialNetwork.Application.Users.Responses;
using SocialNetwork.Tests.Infrastructure;

namespace SocialNetwork.Tests.Integration;

[Collection(ApiCollection.Name)]
public class UsersTests
{
    private readonly ApiFactory _factory;

    public UsersTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, Application.Auth.Responses.AuthResponse Auth)> CreateUser()
    {
        var auth = await _factory.CreateClient().Register();
        return (_factory.CreateClient().Authorize(auth.AccessToken), auth);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ReturnsOwnProfile()
    {
        var (client, auth) = await CreateUser();

        var me = await client.GetFromJsonAsync<Envelope<UserProfileResponse>>("/users/me");

        Assert.Equal(auth.User.Id, me!.Data!.Id);
        Assert.Equal(auth.User.Username, me.Data.Username);
    }

    [Fact]
    public async Task UpdateMe_ChangesProfile()
    {
        var (client, _) = await CreateUser();
        var newUsername = $"new_{Guid.NewGuid():N}"[..20];

        var response = await client.PutAsJsonAsync("/users/me", new
        {
            username = newUsername,
            displayName = "Олег",
            bio = "Привіт!",
            websiteUrl = "https://example.com",
            isPrivate = true,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.ReadEnvelope<UserProfileResponse>()).Data!;
        Assert.Equal(newUsername, profile.Username);
        Assert.Equal("Олег", profile.DisplayName);
        Assert.True(profile.IsPrivate);
    }

    [Fact]
    public async Task UpdateMe_UsernameTaken_Returns409()
    {
        var (_, other) = await CreateUser();
        var (client, _) = await CreateUser();

        var response = await client.PutAsJsonAsync("/users/me",
            new { username = other.User.Username, displayName = "X" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_InvalidWebsite_Returns400()
    {
        var (client, auth) = await CreateUser();

        var response = await client.PutAsJsonAsync("/users/me",
            new { username = auth.User.Username, displayName = "X", websiteUrl = "javascript:alert(1)" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("WebsiteUrl", (await response.ReadEnvelope<object>()).Errors!.Keys);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns400()
    {
        var (client, _) = await CreateUser();

        var response = await client.PutAsJsonAsync("/users/me/password",
            new { currentPassword = "Wrong1234", newPassword = "NewPassw0rd" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CurrentPassword", (await response.ReadEnvelope<object>()).Errors!.Keys);
    }

    [Fact]
    public async Task ChangePassword_Valid_AllowsLoginWithNewPassword_AndRevokesSessions()
    {
        var (client, auth) = await CreateUser();

        var response = await client.PutAsJsonAsync("/users/me/password",
            new { currentPassword = TestClient.Password, newPassword = "NewPassw0rd" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var anonymous = _factory.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/auth/login", new { login = auth.User.Email, password = "NewPassw0rd" });
        var oldRefresh = await anonymous.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_ValidImage_SetsAvatarUrl()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/users/me/avatar", ImageForm("image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.ReadEnvelope<UserProfileResponse>()).Data!;
        Assert.StartsWith("https://storage.test/media/avatars/", profile.AvatarUrl);
    }

    [Fact]
    public async Task UploadAvatar_WrongType_Returns400()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/users/me/avatar", ImageForm("text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent ImageForm(string contentType)
    {
        var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", "avatar.png" } };
    }
}
