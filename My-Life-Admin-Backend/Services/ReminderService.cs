using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class ReminderService(IReminderRepository repository) : IReminderService
{
    public async Task<IEnumerable<Reminder>> GetPendingAsync()
    {
        return await repository.GetPendingAsync();
    }

    public async Task<Reminder?> MarkAsSentAsync(int id)
    {
        var reminder = await repository.GetByIdAsync(id);
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

    public async Task<IEnumerable<Reminder>> GetAllAsync()
    {
        return await repository.GetAllAsync();
    }

    public async Task<Reminder?> GetByIdAsync(int id)
    {
        return await repository.GetByIdAsync(id);
    }

    public async Task<Reminder> AddAsync(Reminder reminder)
    {
        await repository.AddAsync(reminder);
        await repository.SaveChangesAsync();
        return reminder;
    }

    public async Task<Reminder?> UpdateAsync(Reminder reminder)
    {
        var existing = await repository.GetByIdAsync(reminder.Id);
        if (existing is null)
        {
            return null;
        }

        repository.Update(reminder);
        await repository.SaveChangesAsync();
        return reminder;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var reminder = await repository.GetByIdAsync(id);
        if (reminder is null)
        {
            return false;
        }

        repository.Delete(reminder);
        await repository.SaveChangesAsync();
        return true;
    }
}
