using SocialNetwork.Application.Users.Requests;
using SocialNetwork.Application.Users.Responses;

namespace SocialNetwork.Application.Users;

/// <summary>Особистий кабінет поточного користувача (/users/me).</summary>
public interface IUserProfileService
{
    Task<UserProfileResponse> GetMe(CancellationToken ct = default);

    Task<UserProfileResponse> UpdateMe(UpdateUserProfileRequest request, CancellationToken ct = default);

    Task ChangePassword(ChangePasswordRequest request, CancellationToken ct = default);

    Task<UserProfileResponse> UploadAvatar(Stream content, string contentType, long length, CancellationToken ct = default);
}
