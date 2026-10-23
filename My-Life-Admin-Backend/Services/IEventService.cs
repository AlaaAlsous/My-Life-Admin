using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IEventService
{
    Task<Event?> GetByIdAsync(int id);
    Task<IEnumerable<Event>> GetAllAsync();
    Task<IEnumerable<Event>> GetBetweenAsync(DateTime start, DateTime end);
    Task<IEnumerable<Event>> GetForMonthAsync(int year, int month);
    Task<Event> CreateAsync(Event newEvent);

    Task<Event?> UpdateAsync(int id, Event changes);

    Task<bool> DeleteAsync(int id);
}
