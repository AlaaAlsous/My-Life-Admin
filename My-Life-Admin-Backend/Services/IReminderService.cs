using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IReminderService
{
    Task<IEnumerable<Reminder>> GetPendingAsync();
    Task<Reminder?> MarkAsSentAsync(int id);
    Task<IEnumerable<Reminder>> GetAllAsync();
    Task<Reminder?> GetByIdAsync(int id);
    Task<Reminder> AddAsync(Reminder reminder);
    Task<Reminder?> UpdateAsync(Reminder reminder);
    Task<bool> DeleteAsync(int id);
}
