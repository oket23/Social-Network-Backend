namespace SocialNetwork.Application.Storage;

/// <summary>
/// Видалення файлу зі сховища, яке не повинно ламати бізнес-операцію, що його викликає
/// (пост/аватар у БД уже змінено). Збій не губиться: лягає в чергу на повторні спроби.
/// </summary>
public interface IFileCleanupService
{
    Task TryDeleteOrEnqueue(string url, CancellationToken ct = default);
}
