using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Domain.Storage;

namespace SocialNetwork.Application.Storage;

public interface IFailedFileDeletionRepository : IRepository<FailedFileDeletion>
{
    /// <summary>
    /// Рядки, для яких настав час повторної спроби і ще не вичерпано ліміт спроб
    /// (відстежені — Attempts/видалення тут же й збережуться).
    /// </summary>
    Task<List<FailedFileDeletion>> GetDue(DateTime now, int maxAttempts, int take, CancellationToken ct = default);
}
