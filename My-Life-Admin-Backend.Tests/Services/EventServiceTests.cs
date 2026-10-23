using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class EventServiceTests
{
    private readonly FakeEventRepository _repository = new();
    private readonly EventService _service;

    public EventServiceTests()
    {
        _service = new EventService(_repository);
    }

    [Fact]
    public async Task CreateAsync_ValidEvent_AddsAndSaves()
    {
        var created = await _service.CreateAsync(new Event { Title = "Passport expires", EventDate = new DateTime(2027, 3, 5) });

        Assert.Contains(created, _repository.Events);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankTitle_ThrowsAndDoesNotSave(string title)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(new Event { Title = title }));

        Assert.Empty(_repository.Events);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task GetBetweenAsync_ReturnsEventsInInterval()
    {
        _repository.Events.Add(new Event { Id = 1, Title = "In", EventDate = new DateTime(2026, 10, 5) });
        _repository.Events.Add(new Event { Id = 2, Title = "Out", EventDate = new DateTime(2026, 12, 5) });

        var result = await _service.GetBetweenAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.Equal(["In"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetBetweenAsync_EndBeforeStart_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetBetweenAsync(new DateTime(2026, 10, 31), new DateTime(2026, 10, 1)));
    }

    [Fact]
    public async Task GetForMonthAsync_CoversWholeMonthAndNothingElse()
    {
        _repository.Events.Add(new Event { Id = 1, Title = "FirstMoment", EventDate = new DateTime(2026, 10, 1, 0, 0, 0) });
        _repository.Events.Add(new Event { Id = 2, Title = "LastMoment", EventDate = new DateTime(2026, 10, 31, 23, 59, 59) });
        _repository.Events.Add(new Event { Id = 3, Title = "PreviousMonth", EventDate = new DateTime(2026, 9, 30, 23, 59, 59) });
        _repository.Events.Add(new Event { Id = 4, Title = "NextMonth", EventDate = new DateTime(2026, 11, 1, 0, 0, 0) });

        var result = await _service.GetForMonthAsync(2026, 10);

        Assert.Equal(["FirstMoment", "LastMoment"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetForMonthAsync_InvalidMonth_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetForMonthAsync(2026, 13));
    }

    [Fact]
    public async Task UpdateAsync_ExistingEvent_UpdatesFieldsAndSaves()
    {
        var existing = new Event { Id = 1, UserId = 5, Title = "Old", Category = "Old", EventDate = new DateTime(2026, 1, 1) };
        _repository.Events.Add(existing);

        var updated = await _service.UpdateAsync(1, new Event
        {
            Title = "New",
            Description = "Details",
            Category = "Travel",
            EventDate = new DateTime(2026, 6, 1),
            DocumentId = 9,
        });

        Assert.Same(existing, updated);
        Assert.Equal("New", existing.Title);
        Assert.Equal("Details", existing.Description);
        Assert.Equal("Travel", existing.Category);
        Assert.Equal(new DateTime(2026, 6, 1), existing.EventDate);
        Assert.Equal(9, existing.DocumentId);
        Assert.Equal(5, existing.UserId);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNullAndDoesNotSave()
    {
        var result = await _service.UpdateAsync(42, new Event { Title = "New" });

        Assert.Null(result);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_BlankTitle_Throws()
    {
        _repository.Events.Add(new Event { Id = 1, Title = "Old" });

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, new Event { Title = " " }));

        Assert.Equal("Old", _repository.Events[0].Title);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEvent_RemovesAndSaves()
    {
        _repository.Events.Add(new Event { Id = 1, Title = "Delete me" });

        var deleted = await _service.DeleteAsync(1);

        Assert.True(deleted);
        Assert.Empty(_repository.Events);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalseAndDoesNotSave()
    {
        var deleted = await _service.DeleteAsync(42);

        Assert.False(deleted);
        Assert.Equal(0, _repository.SaveCount);
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        public List<Event> Events { get; } = [];
        public int SaveCount { get; private set; }

        public Task<Event?> GetByIdAsync(int id) => Task.FromResult(Events.FirstOrDefault(e => e.Id == id));

        public Task<IEnumerable<Event>> GetAllAsync() => Task.FromResult<IEnumerable<Event>>(Events.ToList());

        public Task AddAsync(Event entity)
        {
            Events.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Event entity)
        {
        }

        public void Delete(Event entity) => Events.Remove(entity);

        public Task SaveChangesAsync()
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Event>> GetEventsBetweenAsync(DateTime start, DateTime end) =>
            Task.FromResult<IEnumerable<Event>>(
                Events.Where(e => e.EventDate >= start && e.EventDate <= end).OrderBy(e => e.EventDate).ToList());
    }
}
