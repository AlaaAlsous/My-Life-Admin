using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;
using My_Life_Admin.Storage;

namespace My_Life_Admin.Tests.Services;

public class DocumentEventIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly DocumentService _service;
    private readonly int _userId;

    public DocumentEventIntegrationTests()
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

        var storage = new Mock<IFileStorage>();
        storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("key.pdf");

        _service = new DocumentService(
            new DocumentRepository(_db),
            storage.Object,
            new DocumentEventSynchronizer(new EventRepository(_db), new ReminderRepository(_db)),
            new ReminderRepository(_db));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Document NewDocument(DateTime? expiry) => new()
    {
        UserId = _userId,
        Title = "Passport",
        FileName = "passport.pdf",
        Category = "Identity",
        ExpiryDate = expiry
    };

    private async Task<Document> Create(DateTime? expiry)
    {
        using var content = new MemoryStream([1]);
        return await _service.CreateAsync(NewDocument(expiry), content);
    }

    [Fact]
    public async Task Create_WithExpiryDate_CreatesLinkedEvent()
    {
        var document = await Create(new DateTime(2026, 12, 1));

        var created = await _db.Events.AsNoTracking().SingleAsync();
        Assert.Equal(document.Id, created.DocumentId);
        Assert.Equal(_userId, created.UserId);
        Assert.Equal("Passport expires", created.Title);
        Assert.Equal("Identity", created.Category);
        Assert.Equal(new DateTime(2026, 12, 1, 12, 0, 0), created.EventDate);
    }

    [Fact]
    public async Task Create_WithoutExpiryDate_CreatesNoEvent()
    {
        await Create(null);

        Assert.Empty(await _db.Events.ToListAsync());
    }

    [Fact]
    public async Task Update_ChangedExpiryDate_UpdatesEventInPlace()
    {
        var document = await Create(new DateTime(2026, 12, 1));
        var originalId = (await _db.Events.AsNoTracking().SingleAsync()).Id;

        await _service.UpdateAsync(document.Id, _userId, new Document
        {
            Title = "Passport",
            Category = "Identity",
            ExpiryDate = new DateTime(2027, 6, 15)
        });

        var updated = await _db.Events.AsNoTracking().SingleAsync();
        Assert.Equal(originalId, updated.Id);
        Assert.Equal(new DateTime(2027, 6, 15, 12, 0, 0), updated.EventDate);
    }

    [Fact]
    public async Task Update_AddingExpiryDateToDocument_CreatesEvent()
    {
        var document = await Create(null);

        await _service.UpdateAsync(document.Id, _userId, new Document
        {
            Title = "Passport",
            ExpiryDate = new DateTime(2027, 6, 15)
        });

        Assert.Single(await _db.Events.ToListAsync());
    }

    [Fact]
    public async Task Update_RemovingExpiryDate_DeletesEvent()
    {
        var document = await Create(new DateTime(2026, 12, 1));

        await _service.UpdateAsync(document.Id, _userId, new Document
        {
            Title = "Passport",
            ExpiryDate = null
        });

        Assert.Empty(await _db.Events.ToListAsync());
    }

    [Fact]
    public async Task Delete_RemovesDocumentAndItsEvents()
    {
        var document = await Create(new DateTime(2026, 12, 1));

        await _service.DeleteAsync(document.Id, _userId);

        Assert.Empty(await _db.Events.ToListAsync());
        Assert.Empty(await _db.Documents.ToListAsync());
    }
}
