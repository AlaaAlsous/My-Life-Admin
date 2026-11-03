using System.Net.Mail;

namespace My_Life_Admin.Services;

public interface IEmailSender
{
    Task SendAsync(MailMessage message);
}