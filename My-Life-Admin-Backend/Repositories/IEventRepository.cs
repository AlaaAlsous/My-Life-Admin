using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IEventRepository : IRepository<Event>
{
    Task<IEnumerable<Event>> GetByUserIdAsync(int userId);

    Task<Event?> GetByIdAndUserIdAsync(int id, int userId);

    Task<IReadOnlyList<Event>> GetByDocumentIdAsync(int documentId, int userId);

    Task<IEnumerable<Event>> GetEventsBetweenAsync(
        int userId,
        DateTime start,
        DateTime end);
}
