using Microsoft.AspNetCore.Identity;
using SocialNetwork.Application.Auth;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Application.Common.Exceptions;
using SocialNetwork.Application.Common.Extensions;
using SocialNetwork.Application.Users.Requests;
using SocialNetwork.Application.Users.Responses;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Application.Users;

public class UserProfileService : IUserProfileService
{
    public const long MaxAvatarSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedAvatarTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<User> _userManager;
    private readonly IFileStorage _fileStorage;
    private readonly IAuthService _authService;

    public UserProfileService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        UserManager<User> userManager,
        IFileStorage fileStorage,
        IAuthService authService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _userManager = userManager;
        _fileStorage = fileStorage;
        _authService = authService;
    }

    public async Task<UserProfileResponse> GetMe(CancellationToken ct = default)
    {
        var user = await GetCurrentUser(ct);
        return user.ToProfileResponse();
    }

    public async Task<UserProfileResponse> UpdateMe(UpdateUserProfileRequest request, CancellationToken ct = default)
    {
        var user = await GetCurrentUser(ct);

        if (!string.Equals(user.UserName, request.Username, StringComparison.Ordinal))
        {
            if (await _userRepository.IsUsernameTaken(request.Username, user.Id, ct))
            {
                throw new ConflictException("Це ім'я користувача вже зайняте.",
                    new Dictionary<string, string[]> { ["Username"] = ["Це ім'я користувача вже зайняте."] });
            }

            // UserManager сам нормалізує username (NormalizedUserName) і перевіряє унікальність.
            (await _userManager.SetUserNameAsync(user, request.Username)).ThrowIfFailed();
        }

        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.WebsiteUrl = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl;
        user.IsPrivate = request.IsPrivate;

        await _unitOfWork.SaveChangesAsync(ct);

        return user.ToProfileResponse();
    }

    public async Task ChangePassword(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await GetCurrentUser(ct);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            throw new BadRequestException("Поточний пароль невірний.",
                new Dictionary<string, string[]> { ["CurrentPassword"] = ["Поточний пароль невірний."] });
        }

        result.ThrowIfFailed();

        // Після зміни пароля виходимо з усіх пристроїв (окрім нового входу).
        await _authService.RevokeAllSessions(user.Id, ct);
    }

    public async Task<UserProfileResponse> UploadAvatar(
        Stream content, string contentType, long length, CancellationToken ct = default)
    {
        if (length == 0)
        {
            throw new BadRequestException("Файл порожній.");
        }

        if (length > MaxAvatarSizeBytes)
        {
            throw new BadRequestException("Файл завеликий (максимум 5 МБ).");
        }

        if (!AllowedAvatarTypes.TryGetValue(contentType, out var extension))
        {
            throw new BadRequestException("Дозволені формати: JPEG, PNG, WEBP.");
        }

        var user = await GetCurrentUser(ct);
        var oldAvatarUrl = user.AvatarUrl;

        // Унікальне ім'я файлу: браузер не покаже закешований старий аватар.
        var path = $"avatars/{user.Id}/{Guid.NewGuid():N}{extension}";
        user.AvatarUrl = await _fileStorage.UploadAsync(content, path, contentType, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        if (oldAvatarUrl is not null)
        {
            await _fileStorage.DeleteAsync(oldAvatarUrl, ct);
        }

        return user.ToProfileResponse();
    }

    private async Task<User> GetCurrentUser(CancellationToken ct)
    {
        var user = await _userRepository.GetById(_currentUser.GetCurrentUserId(), ct);
        return user ?? throw new NotFoundException("Користувача не знайдено.");
    }
}
