using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> SearchAsync(string query);

    Task<IEnumerable<Document>> GetExpiringWithinAsync(int days);
}