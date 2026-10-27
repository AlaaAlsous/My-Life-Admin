using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IDocumentService
{
    Task<Document?> GetByIdAsync(int id, int userId);

    Task<IEnumerable<Document>> GetAllAsync(int userId);

    Task<IEnumerable<Document>> SearchAsync(int userId, string query);

    Task<IEnumerable<Document>> GetExpiringWithinAsync(int userId, int days);

    Task<Document> CreateAsync(Document document, Stream content);

    Task<Document?> UpdateAsync(int id, int userId, Document changes);

    Task<bool> DeleteAsync(int id, int userId);

    Task<DocumentFile?> OpenFileAsync(int id, int userId);
}
