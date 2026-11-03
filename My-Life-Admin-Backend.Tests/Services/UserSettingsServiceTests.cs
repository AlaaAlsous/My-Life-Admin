using Moq;
using My_Life_Admin.Models;
using My_Life_Admin.Repositories;
using My_Life_Admin.Services;

namespace My_Life_Admin.Tests.Services;

public class UserSettingsServiceTests
{
    [Fact]
    public async Task GetByUserIdAsync_ReturnsSettings_WhenSettingsExist()
    {
        // Arrange
        var settings = new UserSettings
        {
            Id = 1,
            UserId = 1
        };

        var repositoryMock = new Mock<IUserSettingsRepository>();

        repositoryMock
            .Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(settings);

        var service = new UserSettingsService(repositoryMock.Object);

        // Act
        var result = await service.GetByUserIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(settings.Id, result.Id);
        Assert.Equal(settings.UserId, result.UserId);

        repositoryMock.Verify(
            r => r.GetByUserIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetOrDefaultAsync_ReturnsDefaults_WhenSettingsDoNotExist()
    {
        // Arrange
        var repositoryMock = new Mock<IUserSettingsRepository>();

        repositoryMock
            .Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync((UserSettings?)null);

        var service = new UserSettingsService(repositoryMock.Object);

        // Act
        var result = await service.GetOrDefaultAsync(1);

        // Assert
        Assert.Equal(1, result.UserId);
        Assert.False(result.DarkMode);
        Assert.True(result.EmailNotifications);

        repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<UserSettings>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_CreatesSettings_WhenSettingsDoNotExist()
    {
        // Arrange
        var repositoryMock = new Mock<IUserSettingsRepository>();

        repositoryMock
            .Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync((UserSettings?)null);

        var service = new UserSettingsService(repositoryMock.Object);

        // Act
        var result = await service.SaveAsync(
            1,
            new UserSettings { DarkMode = true, EmailNotifications = false });

        // Assert
        Assert.Equal(1, result.UserId);
        Assert.True(result.DarkMode);
        Assert.False(result.EmailNotifications);

        repositoryMock.Verify(
            r => r.AddAsync(result),
            Times.Once);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSettings_WhenSettingsExist()
    {
        // Arrange
        var existing = new UserSettings
        {
            Id = 5,
            UserId = 1,
            DarkMode = false,
            EmailNotifications = true
        };

        var repositoryMock = new Mock<IUserSettingsRepository>();

        repositoryMock
            .Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(existing);

        var service = new UserSettingsService(repositoryMock.Object);

        // Act
        var result = await service.SaveAsync(
            1,
            new UserSettings { DarkMode = true, EmailNotifications = false });

        // Assert
        Assert.Same(existing, result);
        Assert.Equal(5, result.Id);
        Assert.True(result.DarkMode);
        Assert.False(result.EmailNotifications);

        repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<UserSettings>()),
            Times.Never);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSettingsAndSavesChanges()
    {
        // Arrange
        var settings = new UserSettings
        {
            Id = 1,
            UserId = 1
        };

        var repositoryMock = new Mock<IUserSettingsRepository>();

        var service = new UserSettingsService(repositoryMock.Object);

        // Act
        await service.UpdateAsync(settings);

        // Assert
        repositoryMock.Verify(
            r => r.Update(settings),
            Times.Once);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }
}