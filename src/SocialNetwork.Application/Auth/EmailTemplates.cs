using System.Net;

namespace SocialNetwork.Application.Auth;

internal static class EmailTemplates
{
    public static string ConfirmEmail(string displayName, string link) => Layout(
        $"Вітаємо, {WebUtility.HtmlEncode(displayName)}!",
        "Підтвердіть свою електронну адресу, щоб завершити реєстрацію.",
        "Підтвердити email",
        link);

    public static string ResetPassword(string displayName, string link) => Layout(
        $"Привіт, {WebUtility.HtmlEncode(displayName)}!",
        "Ми отримали запит на скидання пароля. Посилання дійсне обмежений час. Якщо це були не ви — просто проігноруйте лист.",
        "Скинути пароль",
        link);

    private static string Layout(string title, string text, string buttonText, string link)
    {
        var safeLink = WebUtility.HtmlEncode(link);
        return $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto">
              <h2>{title}</h2>
              <p>{text}</p>
              <p><a href="{safeLink}" style="display:inline-block;padding:10px 20px;background:#0095f6;color:#fff;text-decoration:none;border-radius:8px">{buttonText}</a></p>
              <p style="color:#888;font-size:12px">Якщо кнопка не працює, скопіюйте посилання: {safeLink}</p>
            </div>
            """;
    }
}
