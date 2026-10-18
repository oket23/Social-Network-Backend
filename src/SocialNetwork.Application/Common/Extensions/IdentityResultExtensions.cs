using Microsoft.AspNetCore.Identity;
using SocialNetwork.Application.Common.Exceptions;

namespace SocialNetwork.Application.Common.Extensions;

public static class IdentityResultExtensions
{
    /// <summary>
    /// Перетворює помилки Identity на наші винятки:
    /// дублікати username/email → 409, решта (слабкий пароль тощо) → 400 з помилками по полях.
    /// </summary>
    public static void ThrowIfFailed(this IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => GetFieldName(e.Code))
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

        if (result.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            throw new ConflictException("Користувач з такими даними вже існує.", errors);
        }

        throw new BadRequestException("Помилка валідації.", errors);
    }

    private static string GetFieldName(string code) => code switch
    {
        _ when code.StartsWith("Password", StringComparison.Ordinal) => "Password",
        _ when code.Contains("UserName", StringComparison.Ordinal) => "Username",
        _ when code.Contains("Email", StringComparison.Ordinal) => "Email",
        "InvalidToken" => "Token",
        _ => "General",
    };
}
