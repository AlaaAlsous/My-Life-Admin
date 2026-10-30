using Moq;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace My_Life_Admin.Tests.Services;

public class DocumentEventSynchronizerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly DocumentEventSynchronizer _synchronizer;

    public DocumentEventSynchronizerTests()
    {
        _synchronizer = new DocumentEventSynchronizer(_eventRepositoryMock.Object);
    }

    private static Document Passport(int id = 5, DateTime? expiry = null) => new()
    {
        Id = id,
        UserId = 1,
        Title = "Passport",
        FileName = "passport.pdf",
        Category = "Identity",
        ExpiryDate = expiry
    };

    private void SetupLinked(params Event[] events)
    {
        _eventRepositoryMock
            .Setup(r => r.GetByDocumentIdAsync(5, 1))
            .ReturnsAsync(events);
    }

    [Fact]
    public async Task SyncAsync_CreatesEvent_WhenExpiryDateIsSetAndNoEventExists()
    {
        var document = Passport(expiry: new DateTime(2026, 12, 1));
        SetupLinked();

        await _synchronizer.SyncAsync(document);

        _eventRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Event>(e =>
                e.UserId == 1 &&
                e.Document == document &&
                e.Title == "Passport expires" &&
                e.Category == "Identity" &&
                e.EventDate == new DateTime(2026, 12, 1, 12, 0, 0))),
            Times.Once);
    }

    [Fact]
    public async Task SyncAsync_CreatesEventForUnsavedDocument_WithoutLookingUpEvents()
    {
        var document = Passport(id: 0, expiry: new DateTime(2026, 12, 1));

        await _synchronizer.SyncAsync(document);

        _eventRepositoryMock.Verify(r => r.GetByDocumentIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _eventRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_UsesFileName_WhenTitleIsEmpty()
    {
        var document = Passport(expiry: new DateTime(2026, 12, 1));
        document.Title = " ";
        SetupLinked();

        await _synchronizer.SyncAsync(document);

        _eventRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Event>(e => e.Title == "passport.pdf expires")),
            Times.Once);
    }

    [Fact]
    public async Task SyncAsync_UpdatesExistingEvent_WhenExpiryDateChanges()
    {
        var existing = new Event { Id = 9, DocumentId = 5, Title = "Old", Category = "Old", EventDate = new DateTime(2026, 1, 1) };
        SetupLinked(existing);

        await _synchronizer.SyncAsync(Passport(expiry: new DateTime(2027, 3, 4)));

        Assert.Equal("Passport expires", existing.Title);
        Assert.Equal("Identity", existing.Category);
        Assert.Equal(new DateTime(2027, 3, 4, 12, 0, 0), existing.EventDate);
        _eventRepositoryMock.Verify(r => r.Update(existing), Times.Once);
        _eventRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_DeletesEvent_WhenExpiryDateIsRemoved()
    {
        var existing = new Event { Id = 9, DocumentId = 5 };
        SetupLinked(existing);

        await _synchronizer.SyncAsync(Passport(expiry: null));

        _eventRepositoryMock.Verify(r => r.Delete(existing), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_DoesNothing_WhenNoExpiryDateAndNoEvent()
    {
        SetupLinked();

        await _synchronizer.SyncAsync(Passport(expiry: null));

        _eventRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
        _eventRepositoryMock.Verify(r => r.Delete(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_OnlyTouchesTheFirstLinkedEvent()
    {
        var generated = new Event { Id = 9, DocumentId = 5 };
        var manual = new Event { Id = 10, DocumentId = 5, Title = "Manual" };
        SetupLinked(generated, manual);

        await _synchronizer.SyncAsync(Passport(expiry: null));

        _eventRepositoryMock.Verify(r => r.Delete(generated), Times.Once);
        _eventRepositoryMock.Verify(r => r.Delete(manual), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_DeletesAllLinkedEvents()
    {
        var first = new Event { Id = 9, DocumentId = 5 };
        var second = new Event { Id = 10, DocumentId = 5 };
        SetupLinked(first, second);

        await _synchronizer.RemoveAsync(Passport());

        _eventRepositoryMock.Verify(r => r.Delete(first), Times.Once);
        _eventRepositoryMock.Verify(r => r.Delete(second), Times.Once);
    }
}
