using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class DashboardServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetSummaryAsync_ReturnsOnlyCurrentUsersItems_InExpectedOrder()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();

        db.Users.AddRange(
            new User { Id = 1, Name = "A", Email = "a@test.se", PasswordHash = "x" },
            new User { Id = 2, Name = "B", Email = "b@test.se", PasswordHash = "x" });
        db.Documents.AddRange(
            new Document { Id = 1, UserId = 1, Title = "Old", UploadedAt = Now.AddDays(-10) },
            new Document { Id = 2, UserId = 1, Title = "New", UploadedAt = Now.AddDays(-1) },
            new Document { Id = 3, UserId = 2, Title = "Other", UploadedAt = Now });
        db.Events.AddRange(
            new Event { Id = 1, UserId = 1, Title = "Later", EventDate = Now.AddDays(10) },
            new Event { Id = 2, UserId = 1, Title = "Soon", EventDate = Now.AddDays(2) },
            new Event { Id = 3, UserId = 1, Title = "Past", EventDate = Now.AddDays(-2) },
            new Event { Id = 4, UserId = 2, Title = "Other", EventDate = Now.AddDays(1) });
        db.Reminders.AddRange(
            new Reminder { Id = 1, UserId = 1, ReminderDate = Now.AddDays(-1), Status = ReminderStatus.Pending },
            new Reminder { Id = 2, UserId = 1, ReminderDate = Now.AddDays(1), Status = ReminderStatus.Pending },
            new Reminder { Id = 3, UserId = 1, ReminderDate = Now.AddDays(-3), Status = ReminderStatus.Sent },
            new Reminder { Id = 4, UserId = 2, ReminderDate = Now.AddDays(-1), Status = ReminderStatus.Pending });
        await db.SaveChangesAsync();

        var summary = await new DashboardService(db).GetSummaryAsync(1, Now);

        Assert.Equal(new[] { 2, 1 }, summary.RecentDocuments.Select(d => d.Id));
        Assert.Equal(new[] { 2, 1 }, summary.UpcomingEvents.Select(e => e.Id));
        Assert.Equal(new[] { 2, 1 }, summary.LatestReminders.Select(r => r.Id));
    }
}
