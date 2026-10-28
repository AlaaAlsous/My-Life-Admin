using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Repositories;

public class ReminderRepository : Repository<Reminder>, IReminderRepository
{
    private readonly ApplicationDbContext _db;

    public ReminderRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId)
    {
        return await _db.Reminders
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.ReminderDate)
            .ToListAsync();
    }

    public async Task<Reminder?> GetByIdAndUserIdAsync(int id, int userId)
    {
        return await _db.Reminders
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.UserId == userId);
    }

    public async Task<IEnumerable<Reminder>> GetPendingAsync(int userId)
    {
        var now = DateTime.UtcNow;

        return await _db.Reminders
            .AsNoTracking()
            .Where(r =>
                r.UserId == userId &&
                r.Status == ReminderStatus.Pending &&
                r.ReminderDate <= now)
            .OrderBy(r => r.ReminderDate)
            .ToListAsync();
    }
}
