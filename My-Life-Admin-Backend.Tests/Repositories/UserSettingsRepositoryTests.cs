using Microsoft.EntityFrameworkCore;
using My_Life_Admin.Data;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;

namespace My_Life_Admin.Tests.Repositories;

public class UserSettingsRepositoryTests
{
    [Fact]
    public async Task GetByUserIdAsync_ReturnsSettings_WhenSettingsExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "test"
        };

        var settings = new UserSettings
        {
            Id = 1,
            UserId = user.Id,
            User = user
        };

        context.Users.Add(user);
        context.UserSettings.Add(settings);
        await context.SaveChangesAsync();

        var repository = new UserSettingsRepository(context);

        // Act
        var result = await repository.GetByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(settings.Id, result.Id);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNull_WhenSettingsDoNotExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var repository = new UserSettingsRepository(context);

        // Act
        var result = await repository.GetByUserIdAsync(999);

        // Assert
        Assert.Null(result);
    }
}