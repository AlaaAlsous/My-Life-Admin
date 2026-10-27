using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    private readonly ApplicationDbContext _db;

    public EventRepository(ApplicationDbContext db)
        : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Event>> GetByUserIdAsync(int userId)
    {
        return await _db.Events
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAndUserIdAsync(int id, int userId)
    {
        return await _db.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
    }

    public async Task<IEnumerable<Event>> GetEventsBetweenAsync(
        int userId,
        DateTime start,
        DateTime end)
    {
        return await _db.Events
            .AsNoTracking()
            .Where(e =>
                e.UserId == userId &&
                e.EventDate >= start &&
                e.EventDate <= end)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
    }
}