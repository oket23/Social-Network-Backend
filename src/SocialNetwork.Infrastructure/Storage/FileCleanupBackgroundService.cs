using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Application.Storage;

namespace SocialNetwork.Infrastructure.Storage;

/// <summary>
/// Періодично повторює видалення файлів, яке впало з першої спроби (<see cref="FileCleanupService"/>
/// кладе їх у <see cref="SocialNetwork.Domain.Storage.FailedFileDeletion"/>). Експоненційний бекоф;
/// після <see cref="MaxAttempts"/> невдалих спроб рядок лишається в БД для ручного розбору.
/// </summary>
public class FileCleanupBackgroundService : BackgroundService
{
    public const int MaxAttempts = 10;

    private const int BatchSize = 50;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileCleanupBackgroundService> _logger;

    public FileCleanupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<FileCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ProcessDueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка в фоновій службі повторного очищення файлів");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Один прохід черги. Публічний — щоб тест міг викликати його напряму, не чекаючи таймер.</summary>
    public async Task ProcessDueAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFailedFileDeletionRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var due = await repository.GetDue(DateTime.UtcNow, MaxAttempts, BatchSize, ct);
        if (due.Count == 0)
        {
            return;
        }

        foreach (var item in due)
        {
            try
            {
                await fileStorage.DeleteAsync(item.Url, ct);
                repository.Delete(item);
            }
            catch (Exception ex)
            {
                item.Attempts++;
                item.LastError = ex.Message;
                item.NextAttemptAt = DateTime.UtcNow.Add(NextBackoff(item.Attempts));
                _logger.LogWarning(ex, "Повторна спроба видалити {Url} не вдалась (спроба {Attempts})", item.Url, item.Attempts);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static TimeSpan NextBackoff(int attempts)
    {
        var delay = BaseBackoff * Math.Pow(2, attempts - 1);
        return delay < MaxBackoff ? delay : MaxBackoff;
    }
}
