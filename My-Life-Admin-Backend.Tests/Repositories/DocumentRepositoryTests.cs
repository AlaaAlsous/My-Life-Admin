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
        var result = await repository.SearchAsync("Passport");

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
        var result = await repository.GetExpiringWithinAsync(30);

        // Assert
        var documents = result.ToList();

        Assert.Single(documents);
        Assert.Equal(expiringSoon.Id, documents[0].Id);
    }
}