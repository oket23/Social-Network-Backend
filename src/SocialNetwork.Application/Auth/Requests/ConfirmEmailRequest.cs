namespace SocialNetwork.Application.Auth.Requests;

public class ConfirmEmailRequest
{
    public Guid UserId { get; set; }

    /// <summary>Токен з посилання в листі (як є, у форматі base64url).</summary>
    public required string Token { get; set; }
}
