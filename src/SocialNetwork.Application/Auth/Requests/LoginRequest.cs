namespace SocialNetwork.Application.Auth.Requests;

public class LoginRequest
{
    /// <summary>Email або username.</summary>
    public required string Login { get; set; }

    public required string Password { get; set; }
}
