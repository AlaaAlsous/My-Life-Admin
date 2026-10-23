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
    public async Task<IEnumerable<Reminder>> GetPendingAsync()
    {
        return await _db.Reminders
            .Where(r => r.Status == ReminderStatus.Pending)
            .OrderBy(r => r.ReminderDate)
            .ToListAsync();
    }
}
