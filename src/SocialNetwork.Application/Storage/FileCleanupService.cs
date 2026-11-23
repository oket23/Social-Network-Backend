using Microsoft.Extensions.Logging;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Domain.Storage;

namespace SocialNetwork.Application.Storage;

public class FileCleanupService : IFileCleanupService
{
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMinutes(5);

    private readonly IFileStorage _fileStorage;
    private readonly IFailedFileDeletionRepository _failedFileDeletionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FileCleanupService> _logger;

    public FileCleanupService(
        IFileStorage fileStorage,
        IFailedFileDeletionRepository failedFileDeletionRepository,
        IUnitOfWork unitOfWork,
        ILogger<FileCleanupService> logger)
    {
        _fileStorage = fileStorage;
        _failedFileDeletionRepository = failedFileDeletionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task TryDeleteOrEnqueue(string url, CancellationToken ct = default)
    {
        try
        {
            await _fileStorage.DeleteAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не вдалося видалити файл {Url} зі сховища, додаю в чергу повторних спроб", url);

            // Сам запис у чергу теж не повинен ламати виклик: якщо впаде і він — лишається лише лог.
            try
            {
                _failedFileDeletionRepository.Add(new FailedFileDeletion
                {
                    Url = url,
                    LastError = ex.Message,
                    NextAttemptAt = DateTime.UtcNow.Add(FirstRetryDelay),
                });

                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception enqueueEx)
            {
                _logger.LogError(enqueueEx, "Не вдалося додати файл {Url} у чергу повторного очищення", url);
            }
        }
    }
}
