using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using My_Life_Admin.Models;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class ReminderBackgroundServiceTests
{
    [Fact]
    public async Task ProcessRemindersAsync_SendsEmailAndMarksReminderAsSent()
    {
        const int userId = 1;

        var reminder = new Reminder
        {
            Id = 10,
            UserId = userId,
            ReminderDate = DateTime.UtcNow.AddMinutes(-5),
            Status = ReminderStatus.Pending,
            User = new User
            {
                Id = userId,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "hash"
            }
        };

        var reminderServiceMock = new Mock<IReminderService>();

        reminderServiceMock
            .Setup(s => s.GetAllPendingAsync())
            .ReturnsAsync(new List<Reminder> { reminder });

        reminderServiceMock
            .Setup(s => s.MarkAsSentAsync(10, userId))
            .ReturnsAsync(reminder);

        var emailServiceMock = new Mock<IEmailService>();

        var services = new ServiceCollection();

        services.AddScoped(_ => reminderServiceMock.Object);
        services.AddScoped(_ => emailServiceMock.Object);

        using var provider = services.BuildServiceProvider();

        var scopeFactory =
            provider.GetRequiredService<IServiceScopeFactory>();

        var loggerMock =
            new Mock<ILogger<ReminderBackgroundService>>();

        var backgroundService = new ReminderBackgroundService(
            scopeFactory,
            loggerMock.Object);

        await backgroundService.ProcessRemindersAsync();

        emailServiceMock.Verify(
            e => e.SendReminderAsync(
                "test@example.com",
                "Reminder",
                "You have a reminder."),
            Times.Once);

        reminderServiceMock.Verify(
            s => s.MarkAsSentAsync(10, userId),
            Times.Once);
    }

    [Fact]
    public async Task ProcessRemindersAsync_EmailFails_DoesNotMarkReminderAsSent()
    {
        const int userId = 1;

        var reminder = new Reminder
        {
            Id = 10,
            UserId = userId,
            ReminderDate = DateTime.UtcNow.AddMinutes(-5),
            Status = ReminderStatus.Pending,
            User = new User
            {
                Id = userId,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "hash"
            }
        };

        var reminderServiceMock = new Mock<IReminderService>();

        reminderServiceMock
            .Setup(s => s.GetAllPendingAsync())
            .ReturnsAsync(new List<Reminder> { reminder });

        var emailServiceMock = new Mock<IEmailService>();

        emailServiceMock
            .Setup(e => e.SendReminderAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email failed"));

        var services = new ServiceCollection();

        services.AddScoped(_ => reminderServiceMock.Object);
        services.AddScoped(_ => emailServiceMock.Object);

        using var provider = services.BuildServiceProvider();

        var scopeFactory =
            provider.GetRequiredService<IServiceScopeFactory>();

        var loggerMock =
            new Mock<ILogger<ReminderBackgroundService>>();

        var backgroundService = new ReminderBackgroundService(
            scopeFactory,
            loggerMock.Object);

        await backgroundService.ProcessRemindersAsync();

        reminderServiceMock.Verify(
            s => s.MarkAsSentAsync(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }
}