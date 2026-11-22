using SocialNetwork.Domain.Common;

namespace SocialNetwork.Domain.Storage;

/// <summary>
/// Файл, який не вдалося видалити зі сховища одразу (аватар, фото/відео поста).
/// Рядок живе, доки фонова служба не видалить файл успішно або не вичерпає ліміт спроб.
/// </summary>
public class FailedFileDeletion : BaseEntity
{
    public required string Url { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }

    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
}
