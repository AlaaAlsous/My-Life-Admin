using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Storage;

namespace My_Life_Admin.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;

    public DocumentService(IDocumentRepository documentRepository, IFileStorage fileStorage)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Document?> GetByIdAsync(int id, int userId)
    {
        return await _documentRepository.GetByIdForUserAsync(id, userId);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(int userId)
    {
        return await _documentRepository.GetAllForUserAsync(userId);
    }

    public async Task<IEnumerable<Document>> SearchAsync(int userId, string query)
    {
        return await _documentRepository.SearchAsync(userId, query);
    }

    public async Task<IEnumerable<Document>> GetExpiringWithinAsync(int userId, int days)
    {
        return await _documentRepository.GetExpiringWithinAsync(userId, days);
    }

    public async Task<Document> CreateAsync(Document document, Stream content)
    {
        var extension = Path.GetExtension(document.FileName);
        document.FilePath = await _fileStorage.SaveAsync(content, extension);

        try
        {
            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();
        }
        catch
        {
            await _fileStorage.DeleteAsync(document.FilePath);
            throw;
        }

        return document;
    }

    public async Task<Document?> UpdateAsync(int id, int userId, Document changes)
    {
        var document = await _documentRepository.GetByIdForUserAsync(id, userId);

        if (document == null)
        {
            return null;
        }

        document.Title = changes.Title;
        document.Description = changes.Description;
        document.Category = changes.Category;
        document.ExpiryDate = changes.ExpiryDate;

        _documentRepository.Update(document);
        await _documentRepository.SaveChangesAsync();

        return document;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var document = await _documentRepository.GetByIdForUserAsync(id, userId);

        if (document == null)
        {
            return false;
        }

        _documentRepository.Delete(document);
        await _documentRepository.SaveChangesAsync();
        await _fileStorage.DeleteAsync(document.FilePath);

        return true;
    }

    public async Task<DocumentFile?> OpenFileAsync(int id, int userId)
    {
        var document = await _documentRepository.GetByIdForUserAsync(id, userId);

        if (document == null)
        {
            return null;
        }

        var content = await _fileStorage.OpenReadAsync(document.FilePath);

        return content == null ? null : new DocumentFile(document, content);
    }
}
