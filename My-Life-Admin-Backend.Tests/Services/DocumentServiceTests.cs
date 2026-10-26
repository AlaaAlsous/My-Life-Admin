using Moq;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace My_Life_Admin.Tests.Services;

public class DocumentServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsDocument_WhenDocumentExists()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        };

        var repositoryMock = new Mock<IDocumentRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(document);

        var service = new DocumentService(repositoryMock.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.Title, result.Title);
    }

    [Fact]
    public async Task AddAsync_AddsDocumentAndSavesChanges()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        };

        var repositoryMock = new Mock<IDocumentRepository>();

        var service = new DocumentService(repositoryMock.Object);

        // Act
        await service.AddAsync(document);

        // Assert
        repositoryMock.Verify(
            r => r.AddAsync(document),
            Times.Once);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDocumentAndSavesChanges()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Updated Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        };

        var repositoryMock = new Mock<IDocumentRepository>();

        var service = new DocumentService(repositoryMock.Object);

        // Act
        await service.UpdateAsync(document);

        // Assert
        repositoryMock.Verify(
            r => r.Update(document),
            Times.Once);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DeletesDocumentAndSavesChanges_WhenDocumentExists()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        };

        var repositoryMock = new Mock<IDocumentRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(document);

        var service = new DocumentService(repositoryMock.Object);

        // Act
        await service.DeleteAsync(1);

        // Assert
        repositoryMock.Verify(
            r => r.GetByIdAsync(1),
            Times.Once);

        repositoryMock.Verify(
            r => r.Delete(document),
            Times.Once);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotDelete_WhenDocumentDoesNotExist()
    {
        // Arrange
        var repositoryMock = new Mock<IDocumentRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((Document?)null);

        var service = new DocumentService(repositoryMock.Object);

        // Act
        await service.DeleteAsync(1);

        // Assert
        repositoryMock.Verify(
            r => r.GetByIdAsync(1),
            Times.Once);

        repositoryMock.Verify(
            r => r.Delete(It.IsAny<Document>()),
            Times.Never);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ReturnsDocumentsFromRepository()
    {
        // Arrange
        var documents = new List<Document>
    {
        new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        }
    };

        var repositoryMock = new Mock<IDocumentRepository>();

        repositoryMock
            .Setup(r => r.SearchAsync("Passport"))
            .ReturnsAsync(documents);

        var service = new DocumentService(repositoryMock.Object);

        // Act
        var result = await service.SearchAsync("Passport");

        // Assert
        Assert.Single(result);

        repositoryMock.Verify(
            r => r.SearchAsync("Passport"),
            Times.Once);
    }

    [Fact]
    public async Task GetExpiringWithinAsync_ReturnsExpiringDocumentsFromRepository()
    {
        // Arrange
        var documents = new List<Document>
    {
        new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            ExpiryDate = DateTime.UtcNow.AddDays(10),
            UserId = 1
        }
    };

        var repositoryMock = new Mock<IDocumentRepository>();

        repositoryMock
            .Setup(r => r.GetExpiringWithinAsync(30))
            .ReturnsAsync(documents);

        var service = new DocumentService(repositoryMock.Object);

        // Act
        var result = await service.GetExpiringWithinAsync(30);

        // Assert
        Assert.Single(result);

        repositoryMock.Verify(
            r => r.GetExpiringWithinAsync(30),
            Times.Once);
    }
}