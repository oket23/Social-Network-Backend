using SocialNetwork.Application.Auth.Requests;
using SocialNetwork.Application.Auth.Validators;
using SocialNetwork.Application.Users.Requests;
using SocialNetwork.Application.Users.Validators;

namespace SocialNetwork.Tests.Unit;

public class ValidatorTests
{
    [Theory]
    [InlineData("short1A")]       // < 8 символів
    [InlineData("nouppercase1")]  // без великої літери
    [InlineData("NOLOWERCASE1")]  // без малої літери
    [InlineData("NoDigitsHere")]  // без цифри
    public void Register_WeakPassword_IsInvalid(string password)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest
        {
            Email = "a@test.com", Username = "valid_name", DisplayName = "A", Password = password,
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("кирилиця")]
    public void Register_InvalidUsername_IsInvalid(string username)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest
        {
            Email = "a@test.com", Username = username, DisplayName = "A", Password = "Passw0rd!",
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Username));
    }

    [Fact]
    public void Register_ValidRequest_IsValid()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest
        {
            Email = "a@test.com", Username = "oleh.dev_1", DisplayName = "Олег", Password = "Passw0rd!",
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateProfile_TooLongBio_IsInvalid()
    {
        var result = new UpdateUserProfileRequestValidator().Validate(new UpdateUserProfileRequest
        {
            Username = "oleh", DisplayName = "Oleh", Bio = new string('a', 161),
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserProfileRequest.Bio));
    }
}
