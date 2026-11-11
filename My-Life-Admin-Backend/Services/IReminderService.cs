using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IReminderService
{
    Task<IEnumerable<Reminder>> GetPendingAsync(int userId);

    Task<Reminder?> MarkAsSentAsync(int id, int userId);

    Task<Reminder?> MarkAsSkippedAsync(int id, int userId);

    Task<Reminder?> MarkAsFailedAsync(int id, int userId);

    Task<IEnumerable<Reminder>> GetAllAsync(int userId);

    Task<Reminder?> GetByIdAsync(int id, int userId);

    Task<Reminder> AddAsync(Reminder reminder);

    Task<Reminder?> UpdateAsync(
        int id,
        int userId,
        Reminder changes);

    Task<bool> DeleteAsync(int id, int userId);

    Task<IEnumerable<Reminder>> GetAllPendingAsync();
}