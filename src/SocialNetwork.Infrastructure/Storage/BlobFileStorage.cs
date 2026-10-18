using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SocialNetwork.Application.Common.Abstractions;

namespace SocialNetwork.Infrastructure.Storage;

public class BlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private bool _containerReady;

    public BlobFileStorage(IOptions<StorageSettings> settings)
    {
        _container = new BlobContainerClient(settings.Value.ConnectionString, settings.Value.ContainerName);
    }

    public async Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken ct = default)
    {
        await EnsureContainer(ct);

        var blob = _container.GetBlobClient(path);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, ct);

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var uri = new BlobUriBuilder(new Uri(url));

        // Видаляємо лише файли з нашого контейнера (наприклад, не чіпаємо зовнішні URL).
        if (uri.BlobContainerName != _container.Name)
        {
            return;
        }

        await _container.GetBlobClient(uri.BlobName).DeleteIfExistsAsync(cancellationToken: ct);
    }

    private async Task EnsureContainer(CancellationToken ct)
    {
        if (_containerReady)
        {
            return;
        }

        // Публічний доступ на читання файлів (не до списку) — фронт показує аватари/медіа напряму за URL.
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        _containerReady = true;
    }
}
