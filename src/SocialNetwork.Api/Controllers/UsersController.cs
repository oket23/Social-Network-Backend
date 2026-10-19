using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Application.Common.Responses;
using SocialNetwork.Application.Users;
using SocialNetwork.Application.Users.Requests;
using SocialNetwork.Application.Users.Responses;

namespace SocialNetwork.Api.Controllers;

/// <summary>
/// Особистий кабінет. Контролер "тонкий": лише приймає HTTP-запит і викликає сервіс,
/// уся логіка — в UserProfileService.
/// </summary>
[ApiController]
[Authorize]
[Route("users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UsersController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet("me")]
    [ProducesResponseType<ApiSuccessResponse<UserProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var profile = await _userProfileService.GetMe(ct);
        return Ok(new ApiSuccessResponse<UserProfileResponse>(profile));
    }

    [HttpPut("me")]
    [ProducesResponseType<ApiSuccessResponse<UserProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe(UpdateUserProfileRequest request, CancellationToken ct)
    {
        var profile = await _userProfileService.UpdateMe(request, ct);
        return Ok(new ApiSuccessResponse<UserProfileResponse>(profile, "Профіль оновлено."));
    }

    /// <summary>Зміна пароля. Після неї всі refresh-токени відкликаються (вихід з інших пристроїв).</summary>
    [HttpPut("me/password")]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _userProfileService.ChangePassword(request, ct);
        return Ok(new ApiSuccessResponse("Пароль змінено."));
    }

    /// <summary>Завантаження аватара (multipart/form-data, поле "file"; JPEG/PNG/WEBP до 5 МБ).</summary>
    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UserProfileService.MaxAvatarSizeBytes + 64 * 1024)]
    [ProducesResponseType<ApiSuccessResponse<UserProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var profile = await _userProfileService.UploadAvatar(stream, file.ContentType, file.Length, ct);
        return Ok(new ApiSuccessResponse<UserProfileResponse>(profile, "Аватар оновлено."));
    }
}
