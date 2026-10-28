namespace My_Life_Admin.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
