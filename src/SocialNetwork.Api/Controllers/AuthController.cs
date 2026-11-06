using Microsoft.AspNetCore.Authorization;
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

    /// <summary>
    /// Реєстрація через email або номер телефону (рівно один з двох). Одразу повертає токени
    /// і надсилає код підтвердження — листом або SMS, залежно від того, що вказано.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.Register(request, ct);
        var message = request.Email is not null
            ? "Реєстрація успішна. Перевірте пошту, щоб підтвердити email."
            : "Реєстрація успішна. Ми надіслали SMS з кодом підтвердження номера.";
        return Ok(new ApiSuccessResponse<AuthResponse>(result, message));
    }

    /// <summary>Вхід за email, username або номером телефону.</summary>
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

    /// <summary>Підтвердження email кодом з листа. Потрібен вхід: код перевіряється для поточного користувача.</summary>
    [HttpPost("confirm-email")]
    [Authorize]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken ct)
    {
        await _authService.ConfirmEmail(request, ct);
        return Ok(new ApiSuccessResponse("Email підтверджено."));
    }

    /// <summary>Надсилає новий лист з кодом підтвердження email поточному користувачу.</summary>
    [HttpPost("resend-confirmation")]
    [Authorize]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendConfirmation(CancellationToken ct)
    {
        await _authService.ResendConfirmationEmail(ct);
        return Ok(new ApiSuccessResponse("Ми надіслали новий код на вашу пошту."));
    }

    /// <summary>Підтвердження номера телефону кодом з SMS. Потрібен вхід: код перевіряється для поточного користувача.</summary>
    [HttpPost("confirm-phone")]
    [Authorize]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmPhone(ConfirmPhoneRequest request, CancellationToken ct)
    {
        await _authService.ConfirmPhone(request, ct);
        return Ok(new ApiSuccessResponse("Номер телефону підтверджено."));
    }

    /// <summary>Надсилає нову SMS з кодом підтвердження номера поточному користувачу.</summary>
    [HttpPost("resend-phone-confirmation")]
    [Authorize]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendPhoneConfirmation(CancellationToken ct)
    {
        await _authService.ResendPhoneConfirmation(ct);
        return Ok(new ApiSuccessResponse("Ми надіслали новий код на ваш номер телефону."));
    }

    /// <summary>Надсилає лист з кодом для скидання пароля (відповідь однакова, навіть якщо email не зареєстрований).</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(DependencyInjection.AuthRateLimitPolicy)]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPassword(request, ct);
        return Ok(new ApiSuccessResponse("Якщо такий email зареєстрований, ми надіслали на нього лист."));
    }

    /// <summary>Встановлює новий пароль за кодом з листа. Усі сесії користувача завершуються.</summary>
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
