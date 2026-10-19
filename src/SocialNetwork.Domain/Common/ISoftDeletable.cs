namespace SocialNetwork.Domain.Common;

/// <summary>
/// Сутність, яка не видаляється з БД фізично, а позначається датою видалення.
/// Такі записи автоматично відфільтровуються глобальним query filter у DbContext.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
