using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IDocumentService
{
    Task<Document?> GetByIdAsync(int id);

    Task<IEnumerable<Document>> GetAllAsync();

    Task<IEnumerable<Document>> SearchAsync(string query);

    Task<IEnumerable<Document>> GetExpiringWithinAsync(int days);

    Task AddAsync(Document document);

    Task UpdateAsync(Document document);

    Task DeleteAsync(int id);
}