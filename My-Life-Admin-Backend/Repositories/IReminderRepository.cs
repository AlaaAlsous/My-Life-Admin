using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IReminderRepository : IRepository<Reminder>
{
    Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId);

    Task<Reminder?> GetByIdAndUserIdAsync(int id, int userId);

    Task<IEnumerable<Reminder>> GetPendingAsync(int userId);

    Task<IEnumerable<Reminder>> GetAllPendingAsync();
}
