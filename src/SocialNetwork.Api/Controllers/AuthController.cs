using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialNetwork.Application.Auth;
using SocialNetwork.Application.Auth.Requests;
using SocialNetwork.Application.Auth.Responses;
using SocialNetwork.Application.Common.Responses;

namespace SocialNetwork.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Реєстрація. Одразу повертає токени і надсилає лист підтвердження email.</summary>
    [HttpPost("register")]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.Register(request, ct);
        return Ok(new ApiSuccessResponse<AuthResponse>(result, "Реєстрація успішна. Перевірте пошту, щоб підтвердити email."));
    }

    /// <summary>Вхід за email або username.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.Login(request, ct);
        return Ok(new ApiSuccessResponse<AuthResponse>(result));
    }

    /// <summary>Нова пара токенів в обмін на refresh-токен. Старий refresh-токен після цього недійсний.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<ApiSuccessResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.Refresh(request, ct);
        return Ok(new ApiSuccessResponse<AuthResponse>(result));
    }

    /// <summary>Вихід: відкликає переданий refresh-токен.</summary>
    [HttpPost("logout")]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.Logout(request, ct);
        return Ok(new ApiSuccessResponse("Ви вийшли з акаунта."));
    }

    /// <summary>Підтвердження email за посиланням з листа.</summary>
    [HttpGet("confirm-email")]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmEmailRequest request, CancellationToken ct)
    {
        await _authService.ConfirmEmail(request, ct);
        return Ok(new ApiSuccessResponse("Email підтверджено."));
    }

    /// <summary>Надсилає лист зі скиданням пароля (відповідь однакова, навіть якщо email не зареєстрований).</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPassword(request, ct);
        return Ok(new ApiSuccessResponse("Якщо такий email зареєстрований, ми надіслали на нього лист."));
    }

    /// <summary>Встановлює новий пароль за токеном з листа. Усі сесії користувача завершуються.</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await _authService.ResetPassword(request, ct);
        return Ok(new ApiSuccessResponse("Пароль змінено. Увійдіть з новим паролем."));
    }
}
