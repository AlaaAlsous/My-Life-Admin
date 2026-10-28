using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class ReminderService(IReminderRepository repository) : IReminderService
{
    public Task<IEnumerable<Reminder>> GetPendingAsync(int userId)
    {
        return repository.GetPendingAsync(userId);
    }

    public async Task<Reminder?> MarkAsSentAsync(int id, int userId)
    {
        var reminder = await repository.GetByIdAndUserIdAsync(id, userId);

        if (reminder is null)
        {
            return null;
        }

        reminder.Status = ReminderStatus.Sent;
        reminder.SentAt = DateTime.UtcNow;

        repository.Update(reminder);
        await repository.SaveChangesAsync();

        return reminder;
    }

    public Task<IEnumerable<Reminder>> GetAllAsync(int userId)
    {
        return repository.GetByUserIdAsync(userId);
    }

    public Task<Reminder?> GetByIdAsync(int id, int userId)
    {
        return repository.GetByIdAndUserIdAsync(id, userId);
    }

    public async Task<Reminder> AddAsync(Reminder reminder)
    {
        reminder.Status = ReminderStatus.Pending;
        reminder.SentAt = null;

        await repository.AddAsync(reminder);
        await repository.SaveChangesAsync();

        return reminder;
    }

    public async Task<Reminder?> UpdateAsync(
        int id,
        int userId,
        Reminder changes)
    {
        var existing = await repository.GetByIdAndUserIdAsync(id, userId);

        if (existing is null)
        {
            return null;
        }

        existing.EventId = changes.EventId;
        existing.DocumentId = changes.DocumentId;
        existing.ReminderDate = changes.ReminderDate;

        repository.Update(existing);
        await repository.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var reminder = await repository.GetByIdAndUserIdAsync(id, userId);

        if (reminder is null)
        {
            return false;
        }

        repository.Delete(reminder);
        await repository.SaveChangesAsync();

        return true;
    }
}