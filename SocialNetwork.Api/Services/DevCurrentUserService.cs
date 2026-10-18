namespace SocialNetwork.Api.Services;

/// <summary>
/// ТИМЧАСОВА заглушка замість реальної автентифікації.
/// Завжди повертає Id seed-користувача з AppDbContext.
/// Замінити на реалізацію, що читає userId з JWT (claims), коли буде зроблено логін.
/// </summary>
public class DevCurrentUserService : ICurrentUserService
{
    public static readonly Guid DevUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid GetCurrentUserId() => DevUserId;
}
