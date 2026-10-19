namespace SocialNetwork.Application.Common.Abstractions;

/// <summary>
/// Базові операції репозиторію. Методи Add/Update/Delete нічого не пишуть у БД одразу —
/// зміни зберігає IUnitOfWork.SaveChangesAsync.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Повертає сутність, яку відстежує EF (зміни в ній збережуться через UnitOfWork).</summary>
    ValueTask<TEntity?> GetById(Guid id, CancellationToken ct = default);

    void Add(TEntity entity);

    void Update(TEntity entity);

    /// <summary>Для ISoftDeletable ставить DeletedAt, інакше видаляє фізично.</summary>
    void Delete(TEntity entity);
}
