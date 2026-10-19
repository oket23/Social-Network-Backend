namespace SocialNetwork.Application.Users.Requests;

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }

    public required string NewPassword { get; set; }
}
