namespace SocialNetwork.Application.Auth;

internal static class SmsTemplates
{
    // SMS — простий текст без HTML і посилань, на відміну від листів.
    public static string ConfirmPhone(string code) =>
        $"Social Network: ваш код підтвердження — {code}. Дійсний кілька хвилин. Нікому не повідомляйте цей код.";
}
