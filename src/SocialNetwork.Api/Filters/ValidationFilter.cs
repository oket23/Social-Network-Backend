using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SocialNetwork.Api.Filters;

/// <summary>
/// Перед кожним action шукає FluentValidation-валідатор для кожного аргументу (RegisterRequest → RegisterRequestValidator)
/// і, якщо дані некоректні, кидає ValidationException — middleware перетворить її на 400 з помилками по полях.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
