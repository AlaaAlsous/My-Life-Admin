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

    public async Task<IEnumerable<Reminder>> GetByEventIdAsync(int eventId)
    {
        return await _db.Reminders
            .Where(r => r.EventId == eventId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetByDocumentIdAsync(int documentId)
    {
        return await _db.Reminders
            .Where(r => r.DocumentId == documentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetAllPendingAsync()
    {
        var now = DateTime.UtcNow;

        return await _db.Reminders
            .Include(r => r.User)
                .ThenInclude(u => u.Settings)
            .Include(r => r.Event)
            .Include(r => r.Document)
            .Where(r =>
                r.Status == ReminderStatus.Pending &&
                r.ReminderDate <= now)
            .OrderBy(r => r.ReminderDate)
            .ToListAsync();
    }
}
