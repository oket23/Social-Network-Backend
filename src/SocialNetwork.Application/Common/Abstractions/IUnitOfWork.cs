namespace SocialNetwork.Application.Common.Abstractions;

/// <summary>
/// Unit of Work: репозиторії лише додають/змінюють сутності в пам'яті,
/// а зберігає все однією операцією саме UnitOfWork. Так кілька змін або проходять разом, або не проходять зовсім.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Виконує дію в транзакції БД. Потрібно, коли між кроками є кілька окремих збережень
    /// (наприклад, UserManager зберігає сам, а ми ще оновлюємо refresh-токени).
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
