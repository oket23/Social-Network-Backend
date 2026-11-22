using SocialNetwork.Application.Auth.Requests;
using SocialNetwork.Application.Auth.Responses;

namespace SocialNetwork.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> Login(LoginRequest request, CancellationToken ct = default);

    /// <summary>Видає нову пару токенів в обмін на refresh-токен (старий відкликається — ротація).</summary>
    Task<AuthResponse> Refresh(RefreshTokenRequest request, CancellationToken ct = default);

    Task Logout(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>Підтверджує email поточного користувача кодом з листа.</summary>
    Task ConfirmEmail(ConfirmEmailRequest request, CancellationToken ct = default);

    /// <summary>Надсилає поточному користувачу новий лист з кодом підтвердження.</summary>
    Task ResendConfirmationEmail(CancellationToken ct = default);

    /// <summary>Підтверджує номер телефону поточного користувача кодом з SMS.</summary>
    Task ConfirmPhone(ConfirmPhoneRequest request, CancellationToken ct = default);

    /// <summary>Надсилає поточному користувачу нову SMS з кодом підтвердження номера.</summary>
    Task ResendPhoneConfirmation(CancellationToken ct = default);

    /// <summary>Надсилає лист зі скиданням пароля. Нічого не повідомляє, чи існує такий email.</summary>
    Task ForgotPassword(ForgotPasswordRequest request, CancellationToken ct = default);

    Task ResetPassword(ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>Вихід з усіх пристроїв: відкликає всі refresh-токени користувача.</summary>
    Task RevokeAllSessions(Guid userId, CancellationToken ct = default);
}
