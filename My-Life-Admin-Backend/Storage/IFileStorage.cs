namespace My_Life_Admin.Storage;

// Everything that reads or writes document files goes through this interface, so moving to
// Azure Blob Storage only requires:
//   1. TODO(Azure Blob): add an AzureBlobFileStorage class that implements IFileStorage (package Azure.Storage.Blobs).
//   2. TODO(Azure Blob): change the IFileStorage registration in Program.cs to AzureBlobFileStorage.
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
