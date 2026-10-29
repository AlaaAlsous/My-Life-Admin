using System.Net.Mail;

namespace My_Life_Admin.Services;

public class EmailService(
    IConfiguration configuration,
    IEmailSender emailSender) : IEmailService
{
    public async Task SendReminderAsync(
        string email,
        string subject,
        string message)
    {
        var fromEmail = configuration["Email:FromEmail"];

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail!),
            Subject = subject,
            Body = message,
            IsBodyHtml = false
        };

        mailMessage.To.Add(email);

        await emailSender.SendAsync(mailMessage);
    }
}