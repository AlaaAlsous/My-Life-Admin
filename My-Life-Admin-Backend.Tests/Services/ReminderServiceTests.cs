using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class ReminderServiceTests
{
    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingReminders()
    {
        var repository = new FakeReminderRepository();
        repository.Reminders.Add(new Reminder { Id = 1, UserId = 1, Status = ReminderStatus.Pending });
        repository.Reminders.Add(new Reminder { Id = 2, UserId = 1, Status = ReminderStatus.Sent });
        var service = new ReminderService(repository);

        var result = (await service.GetPendingAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task MarkAsSentAsync_ExistingReminder_SetsStatusAndSentAt()
    {
        var repository = new FakeReminderRepository();
        repository.Reminders.Add(new Reminder { Id = 1, UserId = 1, ReminderDate = DateTime.UtcNow, Status = ReminderStatus.Pending });
        var service = new ReminderService(repository);

        var result = await service.MarkAsSentAsync(1);

        Assert.NotNull(result);
        Assert.Equal(ReminderStatus.Sent, result.Status);
        Assert.NotNull(result.SentAt);
        Assert.Equal(ReminderStatus.Sent, repository.Reminders.Single().Status);
    }

    [Fact]
    public async Task MarkAsSentAsync_UnknownId_ReturnsNull()
    {
        var service = new ReminderService(new FakeReminderRepository());

        var result = await service.MarkAsSentAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsAndReturnsReminder()
    {
        var repository = new FakeReminderRepository();
        var service = new ReminderService(repository);

        var result = await service.AddAsync(new Reminder { Id = 1, UserId = 1 });

        Assert.Equal(1, result.Id);
        Assert.Single(repository.Reminders);
    }

    [Fact]
    public async Task UpdateAsync_ExistingReminder_UpdatesAndReturns()
    {
        var repository = new FakeReminderRepository();
        repository.Reminders.Add(new Reminder { Id = 1, UserId = 1, Status = ReminderStatus.Pending });
        var service = new ReminderService(repository);

        var result = await service.UpdateAsync(new Reminder { Id = 1, UserId = 1, Status = ReminderStatus.Failed });

        Assert.NotNull(result);
        Assert.Equal(ReminderStatus.Failed, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_UnknownReminder_ReturnsNull()
    {
        var service = new ReminderService(new FakeReminderRepository());

        var result = await service.UpdateAsync(new Reminder { Id = 999, UserId = 1 });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingReminder_ReturnsTrueAndRemoves()
    {
        var repository = new FakeReminderRepository();
        repository.Reminders.Add(new Reminder { Id = 1, UserId = 1 });
        var service = new ReminderService(repository);

        var result = await service.DeleteAsync(1);

        Assert.True(result);
        Assert.Empty(repository.Reminders);
    }

    [Fact]
    public async Task DeleteAsync_UnknownReminder_ReturnsFalse()
    {
        var service = new ReminderService(new FakeReminderRepository());

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }

    private sealed class FakeReminderRepository : IReminderRepository
    {
        public List<Reminder> Reminders { get; } = [];

        public Task<Reminder?> GetByIdAsync(int id) =>
            Task.FromResult(Reminders.FirstOrDefault(r => r.Id == id));

        public Task<IEnumerable<Reminder>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Reminder>>(Reminders);

        public Task AddAsync(Reminder entity)
        {
            Reminders.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Reminder entity)
        {
            var index = Reminders.FindIndex(r => r.Id == entity.Id);
            if (index >= 0)
            {
                Reminders[index] = entity;
            }
        }

        public void Delete(Reminder entity) =>
            Reminders.Remove(entity);

        public Task SaveChangesAsync() =>
            Task.CompletedTask;

        public Task<IEnumerable<Reminder>> GetPendingAsync() =>
            Task.FromResult<IEnumerable<Reminder>>(Reminders.Where(r => r.Status == ReminderStatus.Pending));
    }
}
