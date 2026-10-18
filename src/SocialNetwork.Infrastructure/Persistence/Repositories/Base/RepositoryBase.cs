using Microsoft.EntityFrameworkCore;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Domain.Common;

namespace SocialNetwork.Infrastructure.Persistence.Repositories.Base;

public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext _context;

    protected RepositoryBase(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Для читання "на показ": EF не відстежує сутності, тому це швидше.</summary>
    protected IQueryable<TEntity> Query => _context.Set<TEntity>().AsNoTracking();

    /// <summary>Для змін: EF відстежує сутності, і SaveChanges збереже правки.</summary>
    protected DbSet<TEntity> Set => _context.Set<TEntity>();

    public virtual ValueTask<TEntity?> GetById(Guid id, CancellationToken ct = default) =>
        Set.FindAsync([id], ct);

    public virtual void Add(TEntity entity) => Set.Add(entity);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    public virtual void Delete(TEntity entity)
    {
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.DeletedAt = DateTime.UtcNow;
        }
        else
        {
            Set.Remove(entity);
        }
    }
}
