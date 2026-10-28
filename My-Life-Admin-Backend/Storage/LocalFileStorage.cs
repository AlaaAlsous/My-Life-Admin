using Microsoft.Extensions.Options;
using My_Life_Admin.Options;

namespace My_Life_Admin.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;

    private readonly string _rootPath;

    public LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment)
    {
        _rootPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.LocalRootPath)));
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var key = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        await using var file = new FileStream(
            GetPath(key), FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);

        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);

        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true)
            : null;

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_rootPath, key));

        if (string.IsNullOrWhiteSpace(key) || Path.GetDirectoryName(path) != _rootPath)
        {
            throw new ArgumentException("Invalid storage key.", nameof(key));
        }

        return path;
    }
}
