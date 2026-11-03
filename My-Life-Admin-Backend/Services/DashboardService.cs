using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public sealed record DashboardDocument(int Id, string Title, string? Category, DateTime UploadedAt, DateTime? ExpiryDate);

public sealed record DashboardEvent(int Id, string Title, string Category, DateTime EventDate);

public sealed record DashboardReminder(int Id, DateTime ReminderDate, string? Title);

public sealed record DashboardSummary(
    IReadOnlyList<DashboardDocument> RecentDocuments,
    IReadOnlyList<DashboardEvent> UpcomingEvents,
    IReadOnlyList<DashboardReminder> DueReminders);

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(int userId, DateTime now);
}

public class DashboardService(ApplicationDbContext db) : IDashboardService
{
    private const int MaxItems = 5;

    public async Task<DashboardSummary> GetSummaryAsync(int userId, DateTime now)
    {
        var documents = await db.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Take(MaxItems)
            .Select(d => new DashboardDocument(d.Id, d.Title, d.Category, d.UploadedAt, d.ExpiryDate))
            .ToListAsync();

        var events = await db.Events
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.EventDate >= now)
            .OrderBy(e => e.EventDate)
            .Take(MaxItems)
            .Select(e => new DashboardEvent(e.Id, e.Title, e.Category, e.EventDate))
            .ToListAsync();

        var reminders = await db.Reminders
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == ReminderStatus.Pending && r.ReminderDate <= now)
            .OrderBy(r => r.ReminderDate)
            .Take(MaxItems)
            .Select(r => new DashboardReminder(
                r.Id,
                r.ReminderDate,
                r.Event != null ? r.Event.Title : r.Document != null ? r.Document.Title : null))
            .ToListAsync();

        return new DashboardSummary(documents, events, reminders);
    }
}
