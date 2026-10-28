using System.Net;
using System.Net.Mail;

namespace My_Life_Admin.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendReminderAsync(
        string email,
        string subject,
        string message)
    {
        var host = configuration["Email:SmtpHost"];
        var port = int.Parse(configuration["Email:SmtpPort"]!);
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];
        var fromEmail = configuration["Email:FromEmail"];

        using var smtpClient = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(
                username,
                password),
            EnableSsl = true
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail!),
            Subject = subject,
            Body = message,
            IsBodyHtml = false
        };

        mailMessage.To.Add(email);

        await smtpClient.SendMailAsync(mailMessage);
    }
}