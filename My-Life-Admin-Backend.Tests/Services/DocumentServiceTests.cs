using Moq;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;
using My_Life_Admin.Storage;

namespace My_Life_Admin.Tests.Services;

public class DocumentServiceTests
{
    private const int UserId = 1;

    private readonly Mock<IDocumentRepository> _repositoryMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _service = new DocumentService(_repositoryMock.Object, _fileStorageMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDocument_WhenUserOwnsIt()
    {
        // Arrange
        var document = Passport();

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync(document);

        // Act
        var result = await _service.GetByIdAsync(1, UserId);

        // Assert
        Assert.Same(document, result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUsersDocumentsFromRepository()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllForUserAsync(UserId))
            .ReturnsAsync(new List<Document> { Passport() });

        // Act
        var result = await _service.GetAllAsync(UserId);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task SearchAsync_PassesUserIdAndQueryToRepository()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.SearchAsync(UserId, "Passport"))
            .ReturnsAsync(new List<Document> { Passport() });

        // Act
        var result = await _service.SearchAsync(UserId, "Passport");

        // Assert
        Assert.Single(result);

        _repositoryMock.Verify(
            r => r.SearchAsync(UserId, "Passport"),
            Times.Once);
    }

    [Fact]
    public async Task GetExpiringWithinAsync_PassesUserIdAndDaysToRepository()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetExpiringWithinAsync(UserId, 30))
            .ReturnsAsync(new List<Document> { Passport() });

        // Act
        var result = await _service.GetExpiringWithinAsync(UserId, 30);

        // Assert
        Assert.Single(result);

        _repositoryMock.Verify(
            r => r.GetExpiringWithinAsync(UserId, 30),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_StoresFileAndSavesDocumentWithStorageKey()
    {
        // Arrange
        var document = new Document { UserId = UserId, Title = "Passport", FileName = "passport.pdf" };
        using var content = new MemoryStream([1, 2, 3]);

        _fileStorageMock
            .Setup(s => s.SaveAsync(content, ".pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-key.pdf");

        // Act
        var result = await _service.CreateAsync(document, content);

        // Assert
        Assert.Same(document, result);
        Assert.Equal("stored-key.pdf", document.FilePath);

        _repositoryMock.Verify(
            r => r.AddAsync(document),
            Times.Once);

        _repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DeletesStoredFile_WhenSavingDocumentFails()
    {
        // Arrange
        var document = new Document { UserId = UserId, Title = "Passport", FileName = "passport.pdf" };
        using var content = new MemoryStream([1, 2, 3]);

        _fileStorageMock
            .Setup(s => s.SaveAsync(content, ".pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-key.pdf");

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(document, content));

        // Assert
        _fileStorageMock.Verify(
            s => s.DeleteAsync("stored-key.pdf", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMetadataAndSaves_WhenUserOwnsDocument()
    {
        // Arrange
        var document = Passport();

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync(document);

        var changes = new Document
        {
            Title = "New passport",
            Description = "Renewed",
            Category = "Travel",
            ExpiryDate = new DateTime(2036, 1, 1),
        };

        // Act
        var result = await _service.UpdateAsync(1, UserId, changes);

        // Assert
        Assert.Same(document, result);
        Assert.Equal("New passport", document.Title);
        Assert.Equal("Renewed", document.Description);
        Assert.Equal("Travel", document.Category);
        Assert.Equal(new DateTime(2036, 1, 1), document.ExpiryDate);
        Assert.Equal("passport.pdf", document.FileName);
        Assert.Equal("stored-key.pdf", document.FilePath);
        Assert.Equal(UserId, document.UserId);

        _repositoryMock.Verify(
            r => r.Update(document),
            Times.Once);

        _repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenUserDoesNotOwnDocument()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.UpdateAsync(1, UserId, new Document { Title = "New" });

        // Assert
        Assert.Null(result);

        _repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DeletesDocumentAndFile_WhenUserOwnsDocument()
    {
        // Arrange
        var document = Passport();

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync(document);

        // Act
        var result = await _service.DeleteAsync(1, UserId);

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(
            r => r.Delete(document),
            Times.Once);

        _repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);

        _fileStorageMock.Verify(
            s => s.DeleteAsync("stored-key.pdf", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenUserDoesNotOwnDocument()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.DeleteAsync(1, UserId);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(
            r => r.Delete(It.IsAny<Document>()),
            Times.Never);

        _fileStorageMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OpenFileAsync_ReturnsDocumentAndContent_WhenFileExists()
    {
        // Arrange
        var document = Passport();
        using var content = new MemoryStream([1, 2, 3]);

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync(document);

        _fileStorageMock
            .Setup(s => s.OpenReadAsync("stored-key.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _service.OpenFileAsync(1, UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Same(document, result.Document);
        Assert.Same(content, result.Content);
    }

    [Fact]
    public async Task OpenFileAsync_ReturnsNull_WhenFileIsMissing()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync(Passport());

        _fileStorageMock
            .Setup(s => s.OpenReadAsync("stored-key.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _service.OpenFileAsync(1, UserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task OpenFileAsync_ReturnsNull_WithoutReadingStorage_WhenUserDoesNotOwnDocument()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(1, UserId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.OpenFileAsync(1, UserId);

        // Assert
        Assert.Null(result);

        _fileStorageMock.Verify(
            s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Document Passport() => new()
    {
        Id = 1,
        Title = "Passport",
        FileName = "passport.pdf",
        FilePath = "stored-key.pdf",
        UserId = UserId,
    };
}
