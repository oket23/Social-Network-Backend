using FluentValidation;
using SocialNetwork.Application.Posts.Requests;

namespace SocialNetwork.Application.Posts.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Caption).ValidCaption();

        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("Додайте хоча б одне фото чи відео.")
            .Must(files => files.Count <= PostService.MaxMediaCount)
            .WithMessage($"Не більше {PostService.MaxMediaCount} медіафайлів в одному пості.")
            .Must(files => files.Sum(f => f.Length) <= PostMediaFiles.MaxTotalSizeBytes)
            .WithMessage($"Файли поста разом не можуть перевищувати {PostMediaFiles.MaxTotalSizeBytes / 1024 / 1024} МБ.");
    }
}

public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        RuleFor(x => x.Caption).ValidCaption();
    }
}

public class GetPostsRequestValidator : AbstractValidator<GetPostsRequest>
{
    public GetPostsRequestValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, PostService.MaxPageSize)
            .WithMessage($"limit має бути від 1 до {PostService.MaxPageSize}.");
    }
}

internal static class PostValidationRules
{
    public static IRuleBuilderOptions<T, string?> ValidCaption<T>(this IRuleBuilder<T, string?> rule) => rule
        .MaximumLength(PostService.MaxCaptionLength)
        .WithMessage($"Підпис не може бути довшим за {PostService.MaxCaptionLength} символів.");
}
