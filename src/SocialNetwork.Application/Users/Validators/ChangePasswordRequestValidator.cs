using FluentValidation;
using SocialNetwork.Application.Common.Validation;
using SocialNetwork.Application.Users.Requests;

namespace SocialNetwork.Application.Users.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Вкажіть поточний пароль.");

        RuleFor(x => x.NewPassword)
            .StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("Новий пароль має відрізнятися від поточного.");
    }
}
