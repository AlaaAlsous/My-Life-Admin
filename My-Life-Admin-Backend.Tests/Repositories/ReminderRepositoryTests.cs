using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace MyLife_Admin.Tests.Repositories;

public class ReminderRepositoryTests
{
    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingReminders_OrderedByReminderDate()
    {
        using var testDb = CreateDb();
        var db = testDb.Db;
        db.Reminders.AddRange(
            new Reminder { Id = 1, UserId = 1, ReminderDate = new DateTime(2026, 1, 3), Status = ReminderStatus.Sent },
            new Reminder { Id = 2, UserId = 1, ReminderDate = new DateTime(2026, 1, 1), Status = ReminderStatus.Pending },
            new Reminder { Id = 3, UserId = 1, ReminderDate = new DateTime(2026, 1, 2), Status = ReminderStatus.Pending },
            new Reminder { Id = 4, UserId = 1, ReminderDate = new DateTime(2026, 1, 4), Status = ReminderStatus.Failed });
        await db.SaveChangesAsync();

        var repository = new ReminderRepository(db);
        var result = (await repository.GetPendingAsync()).ToList();

        Assert.Equal(new[] { 2, 3 }, result.Select(r => r.Id));
        Assert.All(result, r => Assert.Equal(ReminderStatus.Pending, r.Status));
    }

    private static TestDb CreateDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        db.Users.Add(new User { Id = 1, Name = "Test", Email = "test@example.com" });
        db.SaveChanges();

        return new TestDb(connection, db);
    }

    private sealed class TestDb : IDisposable
    {
        public TestDb(SqliteConnection connection, ApplicationDbContext db)
        {
            Connection = connection;
            Db = db;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Db { get; }

        public void Dispose()
        {
            Db.Dispose();
            Connection.Dispose();
        }
    }
}
