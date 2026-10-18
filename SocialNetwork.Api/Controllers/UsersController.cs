using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Api.Data;
using SocialNetwork.Api.Dtos;
using SocialNetwork.Api.Models;
using SocialNetwork.Api.Services;

namespace SocialNetwork.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private static readonly string[] AllowedAvatarContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IWebHostEnvironment _env;

    public UsersController(
        AppDbContext db,
        ICurrentUserService currentUser,
        IPasswordHasher<User> passwordHasher,
        IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _env = env;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct)
    {
        var user = await _db.Users.FindAsync([_currentUser.GetCurrentUserId()], ct);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(ToDto(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserProfileRequest request, CancellationToken ct)
    {
        var userId = _currentUser.GetCurrentUserId();
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return NotFound();
        }

        var usernameTaken = await _db.Users
            .AnyAsync(u => u.Id != userId && u.Username == request.Username, ct);
        if (usernameTaken)
        {
            ModelState.AddModelError(nameof(request.Username), "Це ім'я користувача вже зайняте.");
            return ValidationProblem(ModelState);
        }

        user.Username = request.Username;
        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.WebsiteUrl = request.WebsiteUrl;
        user.IsPrivate = request.IsPrivate;

        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(user));
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync([_currentUser.GetCurrentUserId()], ct);
        if (user is null)
        {
            return NotFound();
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(request.CurrentPassword), "Поточний пароль невірний.");
            return ValidationProblem(ModelState);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarSizeBytes)]
    public async Task<ActionResult<UserProfileDto>> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Файл порожній.");
            return ValidationProblem(ModelState);
        }

        if (file.Length > MaxAvatarSizeBytes)
        {
            ModelState.AddModelError(nameof(file), "Файл завеликий (максимум 5 МБ).");
            return ValidationProblem(ModelState);
        }

        if (!AllowedAvatarContentTypes.Contains(file.ContentType))
        {
            ModelState.AddModelError(nameof(file), "Дозволені формати: JPEG, PNG, WEBP.");
            return ValidationProblem(ModelState);
        }

        var userId = _currentUser.GetCurrentUserId();
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return NotFound();
        }

        var extension = file.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty,
        };

        var avatarsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(avatarsDir);

        var fileName = $"{userId}{extension}";
        var filePath = Path.Combine(avatarsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        user.AvatarUrl = $"{Request.Scheme}://{Request.Host}/uploads/avatars/{fileName}";
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(user));
    }

    private static UserProfileDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Bio = user.Bio,
        WebsiteUrl = user.WebsiteUrl,
        AvatarUrl = user.AvatarUrl,
        IsPrivate = user.IsPrivate,
        CreatedAt = user.CreatedAt,
    };
}
