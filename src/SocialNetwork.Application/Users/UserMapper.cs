using SocialNetwork.Application.Users.Responses;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Application.Users;

public static class UserMapper
{
    public static UserProfileResponse ToProfileResponse(this User user) => new()
    {
        Id = user.Id,
        Username = user.UserName!,
        Email = user.Email,
        EmailConfirmed = user.EmailConfirmed,
        PhoneNumber = user.PhoneNumber,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        DisplayName = user.DisplayName,
        Bio = user.Bio,
        WebsiteUrl = user.WebsiteUrl,
        AvatarUrl = user.AvatarUrl,
        IsPrivate = user.IsPrivate,
        CreatedAt = user.CreatedAt,
    };
}
