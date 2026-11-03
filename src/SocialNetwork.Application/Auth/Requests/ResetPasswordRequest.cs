namespace SocialNetwork.Application.Auth.Requests;

public class ResetPasswordRequest
{
    public required string Email { get; set; }

    /// <summary>6-значний код з листа.</summary>
    public required string Code { get; set; }

    public required string NewPassword { get; set; }
}
