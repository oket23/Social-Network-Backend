using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SocialNetwork.Application.Auth;
using SocialNetwork.Domain.Users;
using SocialNetwork.Infrastructure.Auth;

namespace SocialNetwork.Tests.Unit;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new(Options.Create(new JwtSettings
    {
        Key = "unit-tests-signing-key-at-least-32-characters",
        AccessTokenMinutes = 15,
    }));

    [Fact]
    public void CreateAccessToken_ContainsUserClaims_AndExpiresIn15Minutes()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "oleh", Email = "oleh@test.com", DisplayName = "Oleh" };

        var (token, expiresAt) = _service.CreateAccessToken(user);
        var jwt = new JsonWebToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal("oleh", jwt.GetClaim(JwtRegisteredClaimNames.UniqueName).Value);
        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueValues()
    {
        Assert.NotEqual(_service.GenerateRefreshToken(), _service.GenerateRefreshToken());
    }

    [Fact]
    public void HashToken_IsDeterministic_AndDoesNotContainOriginal()
    {
        var token = _service.GenerateRefreshToken();

        var hash = _service.HashToken(token);

        Assert.Equal(hash, _service.HashToken(token));
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain(token, hash);
    }
}
