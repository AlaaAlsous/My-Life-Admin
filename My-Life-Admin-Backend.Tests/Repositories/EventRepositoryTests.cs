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

        var user = new User
        {
            Name = "Anna",
            Email = "anna@example.com",
            PasswordHash = "hash"
        };

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

        var result = await _repository.GetEventsBetweenAsync(
            _userId,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31, 23, 59, 59));

        Assert.Equal(["Inside"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_IncludesEventsOnTheBoundaries()
    {
        var start = new DateTime(2026, 10, 1);
        var end = new DateTime(2026, 10, 31, 23, 59, 59);

        await Seed(
            Event("AtStart", start),
            Event("AtEnd", end));

        var result = await _repository.GetEventsBetweenAsync(
            _userId,
            start,
            end);

        Assert.Equal(
            ["AtStart", "AtEnd"],
            result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_ReturnsEventsOrderedByDate()
    {
        await Seed(
            Event("Third", new DateTime(2026, 10, 20)),
            Event("First", new DateTime(2026, 10, 2)),
            Event("Second", new DateTime(2026, 10, 10)));

        var result = await _repository.GetEventsBetweenAsync(
            _userId,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));

        Assert.Equal(
            ["First", "Second", "Third"],
            result.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_NoEventsInRange_ReturnsEmpty()
    {
        await Seed(
            Event("Elsewhere", new DateTime(2026, 1, 1)));

        var result = await _repository.GetEventsBetweenAsync(
            _userId,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_DoesNotReturnOtherUsersEvents()
    {
        var otherUser = new User
        {
            Name = "Bob",
            Email = "bob@example.com",
            PasswordHash = "hash"
        };

        _db.Users.Add(otherUser);
        await _db.SaveChangesAsync();

        await Seed(
            Event("My Event", new DateTime(2026, 10, 10)),
            new Event
            {
                UserId = otherUser.Id,
                Title = "Other User Event",
                Category = "Test",
                EventDate = new DateTime(2026, 10, 10)
            });

        var result = await _repository.GetEventsBetweenAsync(
            _userId,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));

        Assert.Equal(
            ["My Event"],
            result.Select(e => e.Title));
    }

    [Fact]
    public async Task AddAndGetById_RoundTripsEvent()
    {
        var newEvent = Event(
            "Passport expires",
            new DateTime(2027, 3, 5));

        await _repository.AddAsync(newEvent);
        await _repository.SaveChangesAsync();

        var loaded = await _repository.GetByIdAsync(newEvent.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Passport expires", loaded.Title);
    }

    [Fact]
    public async Task GetByDocumentIdAsync_ReturnsOnlyThisUsersEventsForTheDocument()
    {
        var document = new Document { UserId = _userId, Title = "Passport", FileName = "p.pdf" };
        var otherDocument = new Document { UserId = _userId, Title = "Other", FileName = "o.pdf" };
        _db.Documents.AddRange(document, otherDocument);
        await _db.SaveChangesAsync();

        var linked = Event("Linked", new DateTime(2026, 10, 10));
        linked.DocumentId = document.Id;
        var unrelated = Event("Unrelated", new DateTime(2026, 10, 11));
        unrelated.DocumentId = otherDocument.Id;
        await Seed(linked, unrelated, Event("No document", new DateTime(2026, 10, 12)));

        var result = await _repository.GetByDocumentIdAsync(document.Id, _userId);

        Assert.Equal(["Linked"], result.Select(e => e.Title));
        Assert.Empty(await _repository.GetByDocumentIdAsync(document.Id, _userId + 1));
    }

    private Event Event(string title, DateTime date) =>
        new()
        {
            UserId = _userId,
            Title = title,
            Category = "Test",
            EventDate = date
        };

    private async Task Seed(params Event[] events)
    {
        _db.Events.AddRange(events);
        await _db.SaveChangesAsync();
    }
}