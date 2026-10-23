using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace MyLife_Admin.Tests.Repositories;

public class EventRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly EventRepository _repository;
    private readonly int _userId;

    public EventRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        var user = new User { Name = "Anna", Email = "anna@example.com", PasswordHash = "hash" };
        _db.Users.Add(user);
        _db.SaveChanges();
        _userId = user.Id;

        _repository = new EventRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetEventsBetweenAsync_ReturnsOnlyEventsInRange()
    {
        await Seed(
            Event("Before", new DateTime(2026, 9, 30, 23, 59, 0)),
            Event("Inside", new DateTime(2026, 10, 15, 12, 0, 0)),
            Event("After", new DateTime(2026, 11, 1, 0, 0, 1)));

        var result = await _repository.GetEventsBetweenAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31, 23, 59, 59));

        Assert.Equal(["Inside"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_IncludesEventsOnTheBoundaries()
    {
        var start = new DateTime(2026, 10, 1);
        var end = new DateTime(2026, 10, 31, 23, 59, 59);
        await Seed(Event("AtStart", start), Event("AtEnd", end));

        var result = await _repository.GetEventsBetweenAsync(start, end);

        Assert.Equal(["AtStart", "AtEnd"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_ReturnsEventsOrderedByDate()
    {
        await Seed(
            Event("Third", new DateTime(2026, 10, 20)),
            Event("First", new DateTime(2026, 10, 2)),
            Event("Second", new DateTime(2026, 10, 10)));

        var result = await _repository.GetEventsBetweenAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.Equal(["First", "Second", "Third"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_NoEventsInRange_ReturnsEmpty()
    {
        await Seed(Event("Elsewhere", new DateTime(2026, 1, 1)));

        var result = await _repository.GetEventsBetweenAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAndGetById_RoundTripsEvent()
    {
        var newEvent = Event("Passport expires", new DateTime(2027, 3, 5));

        await _repository.AddAsync(newEvent);
        await _repository.SaveChangesAsync();
        var loaded = await _repository.GetByIdAsync(newEvent.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Passport expires", loaded.Title);
    }

    private Event Event(string title, DateTime date) =>
        new() { UserId = _userId, Title = title, Category = "Test", EventDate = date };

    private async Task Seed(params Event[] events)
    {
        _db.Events.AddRange(events);
        await _db.SaveChangesAsync();
    }
}
