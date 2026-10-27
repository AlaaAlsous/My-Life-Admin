using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Tests.Repositories;

public class DocumentRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsDocument_WhenDocumentExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var document = new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.Title, result.Title);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingDocuments()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var passport = new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            UserId = 1
        };

        var insurance = new Document
        {
            Id = 2,
            Title = "Home Insurance",
            FileName = "insurance.pdf",
            FilePath = "/documents/insurance.pdf",
            UserId = 1
        };

        context.Documents.AddRange(passport, insurance);
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.SearchAsync(1, "Passport");

        // Assert
        var documents = result.ToList();

        Assert.Single(documents);
        Assert.Equal(passport.Id, documents[0].Id);
        Assert.Equal(passport.Title, documents[0].Title);
    }

    [Fact]
    public async Task GetExpiringWithinAsync_ReturnsDocumentsExpiringWithinDays()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var expiringSoon = new Document
        {
            Id = 1,
            Title = "Passport",
            FileName = "passport.pdf",
            FilePath = "/documents/passport.pdf",
            ExpiryDate = DateTime.UtcNow.AddDays(10),
            UserId = 1
        };

        var expiringLater = new Document
        {
            Id = 2,
            Title = "Insurance",
            FileName = "insurance.pdf",
            FilePath = "/documents/insurance.pdf",
            ExpiryDate = DateTime.UtcNow.AddDays(60),
            UserId = 1
        };

        context.Documents.AddRange(expiringSoon, expiringLater);
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetExpiringWithinAsync(1, 30);

        // Assert
        var documents = result.ToList();

        Assert.Single(documents);
        Assert.Equal(expiringSoon.Id, documents[0].Id);
    }

    [Fact]
    public async Task GetByIdForUserAsync_ReturnsDocument_WhenUserOwnsIt()
    {
        // Arrange
        await using var context = CreateContext();
        context.Documents.Add(NewDocument(1, userId: 1, "Passport"));
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetByIdForUserAsync(1, userId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Passport", result.Title);
    }

    [Fact]
    public async Task GetByIdForUserAsync_ReturnsNull_ForOtherUsersDocument()
    {
        // Arrange
        await using var context = CreateContext();
        context.Documents.Add(NewDocument(1, userId: 2, "Passport"));
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetByIdForUserAsync(1, userId: 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllForUserAsync_ReturnsOnlyUsersDocuments_NewestFirst()
    {
        // Arrange
        await using var context = CreateContext();

        var older = NewDocument(1, userId: 1, "Older");
        older.UploadedAt = new DateTime(2026, 1, 1);

        var newer = NewDocument(2, userId: 1, "Newer");
        newer.UploadedAt = new DateTime(2026, 6, 1);

        var otherUsers = NewDocument(3, userId: 2, "Other user's");

        context.Documents.AddRange(older, newer, otherUsers);
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetAllForUserAsync(1);

        // Assert
        Assert.Equal(["Newer", "Older"], result.Select(d => d.Title));
    }

    [Fact]
    public async Task SearchAsync_DoesNotReturnOtherUsersDocuments()
    {
        // Arrange
        await using var context = CreateContext();
        context.Documents.AddRange(
            NewDocument(1, userId: 1, "Passport"),
            NewDocument(2, userId: 2, "Passport"));
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.SearchAsync(1, "Passport");

        // Assert
        var document = Assert.Single(result);
        Assert.Equal(1, document.Id);
    }

    [Fact]
    public async Task GetExpiringWithinAsync_DoesNotReturnOtherUsersDocuments()
    {
        // Arrange
        await using var context = CreateContext();

        var mine = NewDocument(1, userId: 1, "Mine");
        mine.ExpiryDate = DateTime.UtcNow.AddDays(10);

        var theirs = NewDocument(2, userId: 2, "Theirs");
        theirs.ExpiryDate = DateTime.UtcNow.AddDays(10);

        context.Documents.AddRange(mine, theirs);
        await context.SaveChangesAsync();

        var repository = new DocumentRepository(context);

        // Act
        var result = await repository.GetExpiringWithinAsync(1, 30);

        // Assert
        var document = Assert.Single(result);
        Assert.Equal(1, document.Id);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Document NewDocument(int id, int userId, string title) => new()
    {
        Id = id,
        UserId = userId,
        Title = title,
        FileName = "file.pdf",
        FilePath = $"{id}.pdf",
    };
}