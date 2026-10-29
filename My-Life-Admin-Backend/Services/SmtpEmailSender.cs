using System.Net;
using System.Net.Mail;

namespace My_Life_Admin.Services;

public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(MailMessage message)
    {
        var host = configuration["Email:SmtpHost"];
        var port = int.Parse(configuration["Email:SmtpPort"]!);
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];

        using var smtpClient = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        await smtpClient.SendMailAsync(message);
    }
}