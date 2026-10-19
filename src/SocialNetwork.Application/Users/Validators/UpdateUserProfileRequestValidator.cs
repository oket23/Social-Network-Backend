using FluentValidation;
using SocialNetwork.Application.Common.Validation;
using SocialNetwork.Application.Users.Requests;

namespace SocialNetwork.Application.Users.Validators;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.Username).ValidUsername();

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Ім'я обов'язкове.")
            .MaximumLength(50).WithMessage("Ім'я не може бути довшим за 50 символів.");

        RuleFor(x => x.Bio)
            .MaximumLength(160).WithMessage("Опис не може бути довшим за 160 символів.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(200).WithMessage("Посилання не може бути довшим за 200 символів.")
            .Must(BeValidHttpUrl).WithMessage("Посилання має бути коректним URL (http/https).")
            .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl));
    }

    private static bool BeValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
