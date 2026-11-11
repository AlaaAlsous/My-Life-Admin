using My_Life_Admin.Models;

namespace My_Life_Admin.Services;

public class ReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessRemindersAsync();

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }

    public async Task ProcessRemindersAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var reminderService =
                scope.ServiceProvider
                    .GetRequiredService<IReminderService>();

            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<IEmailService>();

            var reminders =
                await reminderService.GetAllPendingAsync();

            foreach (var reminder in reminders)
            {
                try
                {
                    if (reminder.User.Settings is { EmailNotifications: false })
                    {
                        await reminderService.MarkAsSkippedAsync(
                            reminder.Id,
                            reminder.UserId);

                        continue;
                    }

                    await emailService.SendReminderAsync(
                        reminder.User.Email,
                        "Reminder",
                        CreateMessage(reminder));

                    await reminderService.MarkAsSentAsync(
                        reminder.Id,
                        reminder.UserId);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to send reminder {ReminderId}",
                        reminder.Id);

                    await reminderService.MarkAsFailedAsync(
                        reminder.Id,
                        reminder.UserId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to process reminders");
        }
    }

    private static string CreateMessage(Reminder reminder)
    {
        if (reminder.Event is not null)
        {
            return $"Reminder for event: {reminder.Event.Title}";
        }

        if (reminder.Document is not null)
        {
            return $"Reminder for document: {reminder.Document.Title}";
        }

        return "You have a reminder.";
    }
}