namespace SocialNetwork.Application.Auth.Requests;

public class RegisterRequest
{
    /// <summary>Email або PhoneNumber — рівно один з двох способів реєстрації.</summary>
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public required string Username { get; set; }

    public required string DisplayName { get; set; }

    public required string Password { get; set; }
}
