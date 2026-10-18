namespace SocialNetwork.Application.Auth.Requests;

public class ResetPasswordRequest
{
    public required string Email { get; set; }

    /// <summary>Токен з посилання в листі (як є, у форматі base64url).</summary>
    public required string Token { get; set; }

    public required string NewPassword { get; set; }
}
