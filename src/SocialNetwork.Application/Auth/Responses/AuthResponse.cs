using SocialNetwork.Application.Users.Responses;

namespace SocialNetwork.Application.Auth.Responses;

public class AuthResponse
{
    public required string AccessToken { get; set; }

    public DateTime AccessTokenExpiresAt { get; set; }

    public required string RefreshToken { get; set; }

    public DateTime RefreshTokenExpiresAt { get; set; }

    public required UserProfileResponse User { get; set; }
}
