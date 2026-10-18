namespace SocialNetwork.Application.Users.Responses;

public class UserProfileResponse
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string Email { get; set; }

    public bool EmailConfirmed { get; set; }

    public required string DisplayName { get; set; }

    public string? Bio { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; }
}
