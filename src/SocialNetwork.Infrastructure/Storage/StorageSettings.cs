namespace SocialNetwork.Infrastructure.Storage;

/// <summary>
/// Секція "Storage". Локально ConnectionString = "UseDevelopmentStorage=true" (Azurite у Docker),
/// в Azure — рядок підключення Storage Account.
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    public string ContainerName { get; set; } = "media";
}
