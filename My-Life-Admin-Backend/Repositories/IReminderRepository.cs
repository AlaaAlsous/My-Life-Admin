using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IReminderRepository : IRepository<Reminder>
{
    Task<IEnumerable<Reminder>> GetPendingAsync();
}
