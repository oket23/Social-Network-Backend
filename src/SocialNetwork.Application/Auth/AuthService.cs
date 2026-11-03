using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialNetwork.Application.Auth.Requests;
using SocialNetwork.Application.Auth.Responses;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Application.Common.Exceptions;
using SocialNetwork.Application.Common.Extensions;
using SocialNetwork.Application.Users;
using SocialNetwork.Domain.Auth;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Application.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Невірний логін або пароль.";
    private const string InvalidRefreshTokenMessage = "Недійсний refresh-токен.";
    private const string InvalidCodeMessage = "Код недійсний або прострочений.";

    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly JwtSettings _jwtSettings;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<JwtSettings> jwtSettings,
        ICurrentUserService currentUser,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _jwtSettings = jwtSettings.Value;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = request.Email,
            UserName = request.Username,
            DisplayName = request.DisplayName,
        };

        // UserManager хешує пароль, перевіряє унікальність email/username і зберігає користувача.
        (await _userManager.CreateAsync(user, request.Password)).ThrowIfFailed();

        await SendConfirmationEmail(user, ct);

        return await IssueTokens(user, ct);
    }

    public async Task<AuthResponse> Login(LoginRequest request, CancellationToken ct = default)
    {
        var user = request.Login.Contains('@')
            ? await _userManager.FindByEmailAsync(request.Login)
            : await _userManager.FindByNameAsync(request.Login);

        // Одне й те саме повідомлення для "немає користувача" і "невірний пароль",
        // щоб не можна було перевірити, чи зареєстрований email.
        if (user is null)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Забагато невдалих спроб входу. Спробуйте пізніше.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokens(user, ct);
    }

    public async Task<AuthResponse> Refresh(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var storedToken = await _refreshTokenRepository.GetByHash(_jwtTokenService.HashToken(request.RefreshToken), ct)
            ?? throw new UnauthorizedException(InvalidRefreshTokenMessage);

        if (storedToken.IsRevoked)
        {
            // Хтось використав уже відкликаний токен — ймовірно, його вкрали.
            // Відкликаємо всі сесії користувача, щоб викрадений ланцюжок перестав працювати.
            _logger.LogWarning("Повторне використання відкликаного refresh-токена для користувача {UserId}", storedToken.UserId);
            await _refreshTokenRepository.RevokeAllForUser(storedToken.UserId, ct);
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        if (storedToken.IsExpired)
        {
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        // Ротація: старий токен відкликаємо, видаємо новий. Обидві зміни збережуться одним SaveChanges.
        return await IssueTokens(storedToken.User, ct, replacedToken: storedToken);
    }

    public async Task Logout(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var storedToken = await _refreshTokenRepository.GetByHash(_jwtTokenService.HashToken(request.RefreshToken), ct);
        if (storedToken is { IsActive: true })
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task ConfirmEmail(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var user = await GetCurrentUser();
        if (user.EmailConfirmed)
        {
            return;
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Code);
        if (!result.Succeeded)
        {
            throw new BadRequestException(InvalidCodeMessage);
        }
    }

    public async Task ResendConfirmationEmail(CancellationToken ct = default)
    {
        var user = await GetCurrentUser();
        if (user.EmailConfirmed)
        {
            throw new BadRequestException("Email уже підтверджено.");
        }

        await SendConfirmationEmail(user, ct);
    }

    public async Task ForgotPassword(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Нічого не кажемо — інакше можна дізнатися, які email зареєстровані.
            return;
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        await TrySendEmail(user.Email!, "Скидання пароля", EmailTemplates.ResetPassword(user.DisplayName, code), ct);
    }

    public async Task ResetPassword(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new BadRequestException(InvalidCodeMessage);

        // Транзакція: або пароль змінено І всі сесії відкликано, або нічого.
        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            // Після зміни пароля Identity оновлює security stamp — тож цей самий код вдруге вже не спрацює.
            var result = await _userManager.ResetPasswordAsync(user, request.Code, request.NewPassword);
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                throw new BadRequestException(InvalidCodeMessage);
            }

            result.ThrowIfFailed();
            await _refreshTokenRepository.RevokeAllForUser(user.Id, innerCt);
        }, ct);
    }

    public Task RevokeAllSessions(Guid userId, CancellationToken ct = default) =>
        _refreshTokenRepository.RevokeAllForUser(userId, ct);

    private async Task<AuthResponse> IssueTokens(User user, CancellationToken ct, RefreshToken? replacedToken = null)
    {
        var (accessToken, accessExpiresAt) = _jwtTokenService.CreateAccessToken(user);

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtTokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
        };
        _refreshTokenRepository.Add(refreshEntity);

        if (replacedToken is not null)
        {
            replacedToken.RevokedAt = DateTime.UtcNow;
            replacedToken.ReplacedByTokenHash = refreshEntity.TokenHash;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshEntity.ExpiresAt,
            User = user.ToProfileResponse(),
        };
    }

    private async Task SendConfirmationEmail(User user, CancellationToken ct)
    {
        // Завдяки налаштуванню options.Tokens у Infrastructure Identity видає 6-значний код, а не довгий токен.
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await TrySendEmail(user.Email!, "Підтвердження email", EmailTemplates.ConfirmEmail(user.DisplayName, code), ct);
    }

    private async Task<User> GetCurrentUser()
    {
        var user = await _userManager.FindByIdAsync(_currentUser.GetCurrentUserId().ToString());
        return user ?? throw new NotFoundException("Користувача не знайдено.");
    }

    /// <summary>Помилка SMTP не повинна ламати реєстрацію чи відповідь API — лише логуємо.</summary>
    private async Task TrySendEmail(string to, string subject, string body, CancellationToken ct)
    {
        try
        {
            await _emailSender.SendAsync(to, subject, body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Не вдалося надіслати лист \"{Subject}\" на {Email}", subject, to);
        }
    }
}
