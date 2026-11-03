using System.Net;

namespace SocialNetwork.Application.Auth;

internal static class EmailTemplates
{
    public static string ConfirmEmail(string displayName, string code) => Layout(
        $"Вітаємо, {WebUtility.HtmlEncode(displayName)}!",
        "Введіть цей код у застосунку, щоб підтвердити електронну адресу.",
        code);

    public static string ResetPassword(string displayName, string code) => Layout(
        $"Привіт, {WebUtility.HtmlEncode(displayName)}!",
        "Ми отримали запит на скидання пароля. Введіть цей код у застосунку. Якщо це були не ви — просто проігноруйте лист.",
        code);

    // Код — лише цифри, тож кодувати його не потрібно. Посилань у листі немає: застосунок мобільний (Expo),
    // і посилання на сторінку фронту на телефоні не відкриється в застосунку.
    private static string Layout(string title, string text, string code) => $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto">
          <h2>{title}</h2>
          <p>{text}</p>
          <p style="font-size:32px;font-weight:bold;letter-spacing:8px;margin:24px 0">{code}</p>
          <p style="color:#888;font-size:12px">Код дійсний кілька хвилин. Якщо він прострочився, запросіть новий у застосунку.</p>
        </div>
        """;
}
