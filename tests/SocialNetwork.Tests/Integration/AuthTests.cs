using System.Net;
using System.Net.Http.Json;
using System.Web;
using SocialNetwork.Application.Auth.Responses;
using SocialNetwork.Application.Users.Responses;
using SocialNetwork.Tests.Infrastructure;

namespace SocialNetwork.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsTokens_AndSendsConfirmationEmail()
    {
        var auth = await _client.Register();

        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
        Assert.False(auth.User.EmailConfirmed);
        Assert.NotNull(_factory.Emails.GetLastLink(auth.User.Email, "Підтвердження"));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var registration = TestClient.NewRegistration();
        await _client.Register(registration);

        var response = await _client.PostAsJsonAsync("/auth/register", registration);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidData_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/auth/register",
            new { email = "not-an-email", username = "x", displayName = "", password = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadEnvelope<object>();
        Assert.False(body.Success);
        Assert.Contains("Email", body.Errors!.Keys);
        Assert.Contains("Username", body.Errors.Keys);
        Assert.Contains("Password", body.Errors.Keys);
    }

    [Fact]
    public async Task Login_ByEmailAndByUsername_Works()
    {
        var auth = await _client.Register();

        var byEmail = await _client.PostAsJsonAsync("/auth/login", new { login = auth.User.Email, password = TestClient.Password });
        var byUsername = await _client.PostAsJsonAsync("/auth/login", new { login = auth.User.Username, password = TestClient.Password });

        Assert.Equal(HttpStatusCode.OK, byEmail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byUsername.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownUser_ReturnSame401()
    {
        var auth = await _client.Register();

        var wrongPassword = await _client.PostAsJsonAsync("/auth/login", new { login = auth.User.Email, password = "Wrong123" });
        var unknownUser = await _client.PostAsJsonAsync("/auth/login", new { login = "nobody@test.com", password = "Wrong123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(
            (await wrongPassword.ReadEnvelope<object>()).Message,
            (await unknownUser.ReadEnvelope<object>()).Message);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndReuseOfOldTokenRevokesAllSessions()
    {
        var auth = await _client.Register();

        var first = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = (await first.ReadEnvelope<AuthResponse>()).Data!;
        Assert.NotEqual(auth.RefreshToken, rotated.RefreshToken);

        // Старий токен уже відкликаний — повторне використання = ознака крадіжки.
        var reuse = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // Через це відкликається й новий токен.
        var afterReuse = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var auth = await _client.Register();

        var logout = await _client.PostAsJsonAsync("/auth/logout", new { refreshToken = auth.RefreshToken });
        var refresh = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithLinkFromEmail_ConfirmsEmail()
    {
        var auth = await _client.Register();
        var link = _factory.Emails.GetLastLink(auth.User.Email, "Підтвердження");

        var response = await _client.GetAsync($"/auth/confirm-email{link.Query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await _factory.CreateClient().Authorize(auth.AccessToken).GetFromJsonAsync<Envelope<UserProfileResponse>>("/users/me");
        Assert.True(me!.Data!.EmailConfirmed);
    }

    [Fact]
    public async Task ForgotAndResetPassword_ChangesPassword_AndRevokesSessions()
    {
        var auth = await _client.Register();

        var forgot = await _client.PostAsJsonAsync("/auth/forgot-password", new { email = auth.User.Email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        var link = _factory.Emails.GetLastLink(auth.User.Email, "Скидання");
        var token = HttpUtility.ParseQueryString(link.Query)["token"];

        var reset = await _client.PostAsJsonAsync("/auth/reset-password",
            new { email = auth.User.Email, token, newPassword = "NewPassw0rd" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var oldLogin = await _client.PostAsJsonAsync("/auth/login", new { login = auth.User.Email, password = TestClient.Password });
        var newLogin = await _client.PostAsJsonAsync("/auth/login", new { login = auth.User.Email, password = "NewPassw0rd" });
        var oldRefresh = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);

        // Токен скидання одноразовий.
        var reuse = await _client.PostAsJsonAsync("/auth/reset-password",
            new { email = auth.User.Email, token, newPassword = "Another1Pass" });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns200WithoutSendingEmail()
    {
        var response = await _client.PostAsJsonAsync("/auth/forgot-password", new { email = "ghost@test.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
