namespace SocialNetwork.Application.Auth.Requests;

public class ConfirmEmailRequest
{
    /// <summary>6-значний код з листа.</summary>
    public required string Code { get; set; }
}
