using Microsoft.EntityFrameworkCore;
using SocialNetwork.Application.Storage;
using SocialNetwork.Domain.Storage;
using SocialNetwork.Infrastructure.Persistence.Repositories.Base;

namespace SocialNetwork.Infrastructure.Persistence.Repositories;

public class FailedFileDeletionRepository : RepositoryBase<FailedFileDeletion>, IFailedFileDeletionRepository
{
    public FailedFileDeletionRepository(AppDbContext context) : base(context)
    {
    }

    public Task<List<FailedFileDeletion>> GetDue(DateTime now, int maxAttempts, int take, CancellationToken ct = default) =>
        Set.Where(f => f.NextAttemptAt <= now && f.Attempts < maxAttempts)
            .OrderBy(f => f.NextAttemptAt)
            .Take(take)
            .ToListAsync(ct);
}
