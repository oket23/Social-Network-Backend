using FluentValidation;
using SocialNetwork.Application.Auth.Requests;
using SocialNetwork.Application.Common.Validation;

namespace SocialNetwork.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        // Рівно один спосіб реєстрації: email або номер телефону.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) ^ !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Вкажіть email або номер телефону (лише один із двох).")
            .WithName("Email");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            RuleFor(x => x.Email!).ValidEmail());

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber!).ValidPhoneNumber());

        RuleFor(x => x.Username).ValidUsername();
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Ім'я обов'язкове.")
            .MaximumLength(50).WithMessage("Ім'я не може бути довшим за 50 символів.");
        RuleFor(x => x.Password).StrongPassword();
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().WithMessage("Вкажіть email або username.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Вкажіть пароль.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh-токен обов'язковий.");
    }
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Code).EmailCode();
    }
}

public class ConfirmPhoneRequestValidator : AbstractValidator<ConfirmPhoneRequest>
{
    public ConfirmPhoneRequestValidator()
    {
        RuleFor(x => x.Code).SmsCode();
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Code).EmailCode();
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}
