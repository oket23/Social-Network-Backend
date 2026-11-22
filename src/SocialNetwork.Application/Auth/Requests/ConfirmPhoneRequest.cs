namespace SocialNetwork.Application.Auth.Requests;

public class ConfirmPhoneRequest
{
    /// <summary>6-значний код з SMS.</summary>
    public required string Code { get; set; }
}
