using System.Net;
using System.Net.Http.Json;
using SocialNetwork.Application.Users.Responses;
using SocialNetwork.Tests.Infrastructure;

namespace SocialNetwork.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PhoneAuthTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public PhoneAuthTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ByPhone_ReturnsTokens_AndSendsSmsCode()
    {
        var auth = await _client.RegisterByPhone();

        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(auth.User.PhoneNumberConfirmed);
        Assert.Matches("^[0-9]{6}$", _factory.Sms.GetLastCode(auth.User.PhoneNumber!));
    }

    [Fact]
    public async Task Register_DuplicatePhone_Returns409()
    {
        var phoneNumber = $"+1555{Random.Shared.Next(1000000, 9999999)}";
        await _client.RegisterByPhone(TestClient.NewPhoneRegistration(phoneNumber));

        var response = await _client.PostAsJsonAsync("/auth/register", TestClient.NewPhoneRegistration(phoneNumber));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_BothEmailAndPhone_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "both@test.com",
            phoneNumber = "+15551234567",
            username = "bothuser",
            displayName = "Both",
            password = TestClient.Password,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_NeitherEmailNorPhone_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            username = "neitheruser",
            displayName = "Neither",
            password = TestClient.Password,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ByPhone_Works()
    {
        var auth = await _client.RegisterByPhone();

        var login = await _client.PostAsJsonAsync("/auth/login",
            new { login = auth.User.PhoneNumber, password = TestClient.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ConfirmPhone_WithCodeFromSms_ConfirmsPhone()
    {
        var auth = await _client.RegisterByPhone();
        var code = _factory.Sms.GetLastCode(auth.User.PhoneNumber!);
        var client = _factory.CreateClient().Authorize(auth.AccessToken);

        var response = await client.PostAsJsonAsync("/auth/confirm-phone", new { code });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await client.GetFromJsonAsync<Envelope<UserProfileResponse>>("/users/me");
        Assert.True(me!.Data!.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task ConfirmPhone_WrongCode_Returns400()
    {
        var auth = await _client.RegisterByPhone();
        var code = _factory.Sms.GetLastCode(auth.User.PhoneNumber!);
        var wrongCode = code == "000000" ? "111111" : "000000";

        var response = await _factory.CreateClient().Authorize(auth.AccessToken)
            .PostAsJsonAsync("/auth/confirm-phone", new { code = wrongCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendPhoneConfirmation_SendsNewSms_AndFailsAfterConfirmation()
    {
        var auth = await _client.RegisterByPhone();
        var client = _factory.CreateClient().Authorize(auth.AccessToken);

        var resend = await client.PostAsync("/auth/resend-phone-confirmation", null);

        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        Assert.Equal(2, _factory.Sms.CountSent(auth.User.PhoneNumber!));

        var code = _factory.Sms.GetLastCode(auth.User.PhoneNumber!);
        await client.PostAsJsonAsync("/auth/confirm-phone", new { code });
        var afterConfirm = await client.PostAsync("/auth/resend-phone-confirmation", null);

        Assert.Equal(HttpStatusCode.BadRequest, afterConfirm.StatusCode);
    }
}
