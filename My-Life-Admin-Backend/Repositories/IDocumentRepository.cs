using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByIdForUserAsync(int id, int userId);

    Task<IEnumerable<Document>> GetAllForUserAsync(int userId);

    Task<IEnumerable<Document>> SearchAsync(int userId, string query);

    Task<IEnumerable<Document>> GetExpiringWithinAsync(int userId, int days);
}
