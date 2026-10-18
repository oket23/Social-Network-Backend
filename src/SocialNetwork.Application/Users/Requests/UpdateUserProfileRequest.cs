namespace SocialNetwork.Application.Users.Requests;

public class UpdateUserProfileRequest
{
    public required string Username { get; set; }

    public required string DisplayName { get; set; }

    public string? Bio { get; set; }

    public string? WebsiteUrl { get; set; }

    public bool IsPrivate { get; set; }
}
