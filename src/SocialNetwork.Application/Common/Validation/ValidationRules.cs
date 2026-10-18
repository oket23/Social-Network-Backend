using FluentValidation;

namespace SocialNetwork.Application.Common.Validation;

/// <summary>
/// Спільні правила валідації, щоб не дублювати їх у кожному валідаторі.
/// Правила пароля мають збігатися з налаштуваннями Identity в Infrastructure/DependencyInjection.cs.
/// </summary>
public static class ValidationRules
{
    public const int PasswordMinLength = 8;

    public static IRuleBuilderOptions<T, string> ValidUsername<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Username обов'язковий.")
        .Length(3, 30).WithMessage("Username має містити від 3 до 30 символів.")
        .Matches("^[a-zA-Z0-9_.]+$").WithMessage("Username може містити лише латинські літери, цифри, '_' та '.'.");

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Пароль обов'язковий.")
        .MinimumLength(PasswordMinLength).WithMessage($"Пароль має містити щонайменше {PasswordMinLength} символів.")
        .MaximumLength(100).WithMessage("Пароль задовгий.")
        .Matches("[A-Z]").WithMessage("Пароль має містити велику літеру.")
        .Matches("[a-z]").WithMessage("Пароль має містити малу літеру.")
        .Matches("[0-9]").WithMessage("Пароль має містити цифру.");

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Email обов'язковий.")
        .MaximumLength(256)
        .EmailAddress().WithMessage("Некоректний email.");
}
