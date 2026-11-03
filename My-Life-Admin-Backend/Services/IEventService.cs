using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public interface IEventService
{
    Task<Event?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<Event>> GetAllAsync(int userId);
    Task<IEnumerable<Event>> GetBetweenAsync(int userId, DateTime start, DateTime end);
    Task<IEnumerable<Event>> GetForMonthAsync(int userId, int year, int month);
    Task<Event> CreateAsync(Event newEvent);

    Task<Event?> UpdateAsync(int id, int userId, Event changes);

    Task<bool> DeleteAsync(int id, int userId);
}
