using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace MyLife_Admin.Tests.Repositories;

public class ReminderRepositoryTests
{
    private const int UserId = 1;

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyDuePendingRemindersForUser_OrderedByReminderDate()
    {
        using var testDb = CreateDb();
        var db = testDb.Db;

        var now = DateTime.UtcNow;

        db.Reminders.AddRange(
            new Reminder
            {
                Id = 1,
                UserId = UserId,
                ReminderDate = now.AddHours(-3),
                Status = ReminderStatus.Sent
            },
            new Reminder
            {
                Id = 2,
                UserId = UserId,
                ReminderDate = now.AddHours(-2),
                Status = ReminderStatus.Pending
            },
            new Reminder
            {
                Id = 3,
                UserId = UserId,
                ReminderDate = now.AddHours(-1),
                Status = ReminderStatus.Pending
            },
            new Reminder
            {
                Id = 4,
                UserId = UserId,
                ReminderDate = now.AddHours(-4),
                Status = ReminderStatus.Failed
            },
            new Reminder
            {
                Id = 5,
                UserId = UserId,
                ReminderDate = now.AddHours(1),
                Status = ReminderStatus.Pending
            });

        await db.SaveChangesAsync();

        var repository = new ReminderRepository(db);

        var result = (
            await repository.GetPendingAsync(UserId))
            .ToList();

        Assert.Equal(
            new[] { 2, 3 },
            result.Select(r => r.Id));

        Assert.All(
            result,
            r => Assert.Equal(
                ReminderStatus.Pending,
                r.Status));
    }

    [Fact]
    public async Task GetPendingAsync_DoesNotReturnOtherUsersReminders()
    {
        using var testDb = CreateDb();
        var db = testDb.Db;

        var otherUser = new User
        {
            Id = 2,
            Name = "Other User",
            Email = "other@example.com",
            PasswordHash = "hash"
        };

        db.Users.Add(otherUser);

        db.Reminders.AddRange(
            new Reminder
            {
                Id = 1,
                UserId = UserId,
                ReminderDate = DateTime.UtcNow.AddMinutes(-10),
                Status = ReminderStatus.Pending
            },
            new Reminder
            {
                Id = 2,
                UserId = 2,
                ReminderDate = DateTime.UtcNow.AddMinutes(-10),
                Status = ReminderStatus.Pending
            });

        await db.SaveChangesAsync();

        var repository = new ReminderRepository(db);

        var result = (
            await repository.GetPendingAsync(UserId))
            .ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(UserId, result[0].UserId);
    }

    [Fact]
    public async Task GetAllPendingAsync_IncludesUserSettings()
    {
        using var testDb = CreateDb();
        var db = testDb.Db;

        db.UserSettings.Add(new UserSettings
        {
            UserId = UserId,
            EmailNotifications = false
        });

        db.Reminders.Add(new Reminder
        {
            Id = 1,
            UserId = UserId,
            ReminderDate = DateTime.UtcNow.AddMinutes(-10),
            Status = ReminderStatus.Pending
        });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new ReminderRepository(db);

        var reminder = Assert.Single(
            await repository.GetAllPendingAsync());

        Assert.NotNull(reminder.User.Settings);
        Assert.False(reminder.User.Settings.EmailNotifications);
    }

    private static TestDb CreateDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

        var db = new ApplicationDbContext(options);

        db.Database.EnsureCreated();

        db.Users.Add(new User
        {
            Id = UserId,
            Name = "Test",
            Email = "test@example.com",
            PasswordHash = "hash"
        });

        db.SaveChanges();

        return new TestDb(connection, db);
    }

    private sealed class TestDb : IDisposable
    {
        public TestDb(
            SqliteConnection connection,
            ApplicationDbContext db)
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