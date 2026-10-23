using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public interface IEventRepository : IRepository<Event>
{
    Task<IEnumerable<Event>> GetEventsBetweenAsync(DateTime start, DateTime end);
}
