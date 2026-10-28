namespace My_Life_Admin.Services;

public interface IEmailService
{
    Task SendReminderAsync(
        string email,
        string subject,
        string message);
}