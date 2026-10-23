using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Services;

public class EventService(IEventRepository repository) : IEventService
{
    public Task<Event?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public Task<IEnumerable<Event>> GetAllAsync() => repository.GetAllAsync();

    public async Task<IEnumerable<Event>> GetBetweenAsync(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ArgumentException("End must not be before start.", nameof(end));
        }

        return await repository.GetEventsBetweenAsync(start, end);
    }

    public async Task<IEnumerable<Event>> GetForMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddTicks(-1);

        return await GetBetweenAsync(start, end);
    }

    public async Task<Event> CreateAsync(Event newEvent)
    {
        ValidateTitle(newEvent);

        await repository.AddAsync(newEvent);
        await repository.SaveChangesAsync();

        return newEvent;
    }

    public async Task<Event?> UpdateAsync(int id, Event changes)
    {
        ValidateTitle(changes);

        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        existing.Title = changes.Title;
        existing.Description = changes.Description;
        existing.Category = changes.Category;
        existing.EventDate = changes.EventDate;
        existing.DocumentId = changes.DocumentId;

        repository.Update(existing);
        await repository.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        repository.Delete(existing);
        await repository.SaveChangesAsync();

        return true;
    }

    private static void ValidateTitle(Event e)
    {
        if (string.IsNullOrWhiteSpace(e.Title))
        {
            throw new ArgumentException("Title is required.", nameof(e));
        }
    }
}
