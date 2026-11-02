using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using My_Life_Admin.Options;

namespace My_Life_Admin.Storage;

public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(IOptions<FileStorageOptions> options, BlobServiceClient blobServiceClient)
    {
        var connectionString = options.Value.ConnectionString;
        var containerName = options.Value.ContainerName;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "FileStorage:ConnectionString is required for Azure Blob Storage.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "FileStorage:ContainerName is required for Azure Blob Storage.");
        }

        _container = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var key = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var blobClient = _container.GetBlobClient(key);

        content.Position = 0;
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);

        return key;
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(key);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var stream = new MemoryStream();
        await blobClient.DownloadToAsync(stream, cancellationToken);
        stream.Position = 0;

        return stream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(key);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
