using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace My_Life_Admin.Tests.Services;

public class DocumentEventBackfillTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly DocumentEventBackfill _backfill;
    private readonly int _userId;

    public DocumentEventBackfillTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        var user = new User { Name = "Anna", Email = "anna@example.com", PasswordHash = "hash" };
        _db.Users.Add(user);
        _db.SaveChanges();
        _userId = user.Id;

        _backfill = new DocumentEventBackfill(_db, NullLogger<DocumentEventBackfill>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Document AddDocument(string title, DateTime? expiry)
    {
        var document = new Document
        {
            UserId = _userId,
            Title = title,
            FileName = $"{title}.pdf",
            Category = "Identity",
            ExpiryDate = expiry
        };
        _db.Documents.Add(document);
        _db.SaveChanges();
        return document;
    }

    [Fact]
    public async Task RunAsync_CreatesEventForDocumentWithExpiryDateAndNoEvent()
    {
        var document = AddDocument("Passport", new DateTime(2026, 12, 1));

        var created = await _backfill.RunAsync();

        Assert.Equal(1, created);
        var eventItem = await _db.Events.AsNoTracking().SingleAsync();
        Assert.Equal(document.Id, eventItem.DocumentId);
        Assert.Equal(_userId, eventItem.UserId);
        Assert.Equal("Passport expires", eventItem.Title);
        Assert.Equal(new DateTime(2026, 12, 1, 12, 0, 0), eventItem.EventDate);
    }

    [Fact]
    public async Task RunAsync_SkipsDocumentsWithoutExpiryDate()
    {
        AddDocument("Receipt", null);

        Assert.Equal(0, await _backfill.RunAsync());
        Assert.Empty(await _db.Events.ToListAsync());
    }

    [Fact]
    public async Task RunAsync_SkipsDocumentsThatAlreadyHaveAnEvent()
    {
        var document = AddDocument("Passport", new DateTime(2026, 12, 1));
        _db.Events.Add(new Event
        {
            UserId = _userId,
            DocumentId = document.Id,
            Title = "Existing",
            Category = "Identity",
            EventDate = new DateTime(2026, 12, 1, 12, 0, 0)
        });
        _db.SaveChanges();

        Assert.Equal(0, await _backfill.RunAsync());
        Assert.Single(await _db.Events.ToListAsync());
    }

    [Fact]
    public async Task RunAsync_IsIdempotent()
    {
        AddDocument("Passport", new DateTime(2026, 12, 1));

        await _backfill.RunAsync();
        var secondRun = await _backfill.RunAsync();

        Assert.Equal(0, secondRun);
        Assert.Single(await _db.Events.ToListAsync());
    }
}
