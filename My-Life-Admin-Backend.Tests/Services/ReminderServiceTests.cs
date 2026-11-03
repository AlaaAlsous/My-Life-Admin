using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class ReminderServiceTests
{
    private const int UserId = 1;

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingRemindersForUser()
    {
        var repository = new FakeReminderRepository();

        repository.Reminders.Add(new Reminder
        {
            Id = 1,
            UserId = UserId,
            Status = ReminderStatus.Pending
        });

        repository.Reminders.Add(new Reminder
        {
            Id = 2,
            UserId = UserId,
            Status = ReminderStatus.Sent
        });

        repository.Reminders.Add(new Reminder
        {
            Id = 3,
            UserId = 2,
            Status = ReminderStatus.Pending
        });

        var service = new ReminderService(repository);

        var result = (await service.GetPendingAsync(UserId)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task MarkAsSentAsync_ExistingReminder_SetsStatusAndSentAt()
    {
        var repository = new FakeReminderRepository();

        repository.Reminders.Add(new Reminder
        {
            Id = 1,
            UserId = UserId,
            ReminderDate = DateTime.UtcNow,
            Status = ReminderStatus.Pending
        });

        var service = new ReminderService(repository);

        var result = await service.MarkAsSentAsync(1, UserId);

        Assert.NotNull(result);
        Assert.Equal(ReminderStatus.Sent, result.Status);
        Assert.NotNull(result.SentAt);
        Assert.Equal(
            ReminderStatus.Sent,
            repository.Reminders.Single().Status);
    }

    [Fact]
    public async Task MarkAsSentAsync_UnknownId_ReturnsNull()
    {
        var service = new ReminderService(
            new FakeReminderRepository());

        var result = await service.MarkAsSentAsync(999, UserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsAndReturnsReminder()
    {
        var repository = new FakeReminderRepository();
        var service = new ReminderService(repository);

        var result = await service.AddAsync(new Reminder
        {
            Id = 1,
            UserId = UserId
        });

        Assert.Equal(1, result.Id);
        Assert.Equal(ReminderStatus.Pending, result.Status);
        Assert.Null(result.SentAt);
        Assert.Single(repository.Reminders);
    }

    [Fact]
    public async Task UpdateAsync_ExistingReminder_UpdatesAndReturns()
    {
        var repository = new FakeReminderRepository();

        repository.Reminders.Add(new Reminder
        {
            Id = 1,
            UserId = UserId,
            EventId = 2,
            ReminderDate = new DateTime(2026, 10, 1),
            Status = ReminderStatus.Pending
        });

        var service = new ReminderService(repository);

        var result = await service.UpdateAsync(
            1,
            UserId,
            new Reminder
            {
                EventId = 3,
                DocumentId = 4,
                ReminderDate = new DateTime(2026, 11, 1)
            });

        Assert.NotNull(result);
        Assert.Equal(3, result.EventId);
        Assert.Equal(4, result.DocumentId);
        Assert.Equal(
            new DateTime(2026, 11, 1),
            result.ReminderDate);

        Assert.Equal(ReminderStatus.Pending, result.Status);
        Assert.Equal(UserId, result.UserId);
    }

    [Fact]
    public async Task UpdateAsync_UnknownReminder_ReturnsNull()
    {
        var service = new ReminderService(
            new FakeReminderRepository());

        var result = await service.UpdateAsync(
            999,
            UserId,
            new Reminder
            {
                ReminderDate = DateTime.UtcNow
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingReminder_ReturnsTrueAndRemoves()
    {
        var repository = new FakeReminderRepository();

        repository.Reminders.Add(new Reminder
        {
            Id = 1,
            UserId = UserId
        });

        var service = new ReminderService(repository);

        var result = await service.DeleteAsync(1, UserId);

        Assert.True(result);
        Assert.Empty(repository.Reminders);
    }

    [Fact]
    public async Task DeleteAsync_UnknownReminder_ReturnsFalse()
    {
        var service = new ReminderService(
            new FakeReminderRepository());

        var result = await service.DeleteAsync(999, UserId);

        Assert.False(result);
    }

    private sealed class FakeReminderRepository : IReminderRepository
    {
        public List<Reminder> Reminders { get; } = [];

        public Task<Reminder?> GetByIdAsync(int id) =>
            Task.FromResult(
                Reminders.FirstOrDefault(r => r.Id == id));

        public Task<IEnumerable<Reminder>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Reminder>>(
                Reminders.ToList());

        public Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId) =>
            Task.FromResult<IEnumerable<Reminder>>(
                Reminders
                    .Where(r => r.UserId == userId)
                    .ToList());

        public Task<Reminder?> GetByIdAndUserIdAsync(
            int id,
            int userId) =>
            Task.FromResult(
                Reminders.FirstOrDefault(r =>
                    r.Id == id &&
                    r.UserId == userId));

        public Task AddAsync(Reminder entity)
        {
            Reminders.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Reminder entity)
        {
        }

        public void Delete(Reminder entity) =>
            Reminders.Remove(entity);

        public Task SaveChangesAsync() =>
            Task.CompletedTask;

        public Task<IEnumerable<Reminder>> GetPendingAsync(int userId) =>
            Task.FromResult<IEnumerable<Reminder>>(
                Reminders
                    .Where(r =>
                        r.UserId == userId &&
                        r.Status == ReminderStatus.Pending)
                    .OrderBy(r => r.ReminderDate)
                    .ToList());

        public Task<IEnumerable<Reminder>> GetAllPendingAsync() =>
            Task.FromResult<IEnumerable<Reminder>>(
                Reminders
                    .Where(r => r.Status == ReminderStatus.Pending)
                    .OrderBy(r => r.ReminderDate)
                    .ToList());
    }
}