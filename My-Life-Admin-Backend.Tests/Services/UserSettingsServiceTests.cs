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