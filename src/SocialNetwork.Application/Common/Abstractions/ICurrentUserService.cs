namespace SocialNetwork.Application.Common.Abstractions;

/// <summary>
/// Дає Id поточного користувача. Реалізація в Api читає його з claims JWT-токена,
/// тож сервіси не залежать від HttpContext.
/// </summary>
public interface ICurrentUserService
{
    Guid GetCurrentUserId();
}
